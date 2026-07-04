using Microsoft.Extensions.Logging;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Domain.Seeds;

namespace MoneyManager.Application.Services;

public interface IOpenBankingCategoryMigrationService
{
    // Migra as categorias do usuário para o padrão Open Banking (idempotente via
    // User.HasMigratedOpenBankingCategories). Retorna true se a migração foi executada nesta chamada.
    Task<bool> MigrateUserToOpenBankingCategoriesAsync(string userId);

    // Resolve o Id da Category ativa do usuário a partir do categoryId do Pluggy.
    // Quando não encontrada, cai na categoria fallback "Outros" (criando-a se necessário).
    Task<string> ResolveUserCategoryIdAsync(string userId, string openBankingCategoryId);

    // Recategoriza transações já sincronizadas sem categoria, usando o categoryId
    // de origem do Pluggy persistido na transação. Retorna o total recategorizado.
    Task<int> RecategorizeExistingTransactionsAsync(string userId);
}

public class OpenBankingCategoryMigrationService : IOpenBankingCategoryMigrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OpenBankingCategoryMigrationService> _logger;

    public OpenBankingCategoryMigrationService(
        IUnitOfWork unitOfWork,
        ILogger<OpenBankingCategoryMigrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> MigrateUserToOpenBankingCategoriesAsync(string userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Usuário não encontrado");

        if (user.HasMigratedOpenBankingCategories)
            return false;

        var categories = (await _unitOfWork.Categories.GetByUserIdAsync(userId)).ToList();

        // Desativa as categorias ativas do usuário que não são do padrão Open Banking.
        var deactivatedCount = 0;
        foreach (var category in categories.Where(c => !c.IsDeleted && string.IsNullOrEmpty(c.OpenBankingCategoryId)))
        {
            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Categories.UpdateAsync(category);
            deactivatedCount++;
        }

        // Cria (ou reativa, se já existir — ex: fallback "Outros" criada antes da migração)
        // uma Category para cada categoria do seed Open Banking.
        var existingByOpenBankingId = categories
            .Where(c => !string.IsNullOrEmpty(c.OpenBankingCategoryId))
            .ToDictionary(c => c.OpenBankingCategoryId!);

        var createdCount = 0;
        foreach (var seed in OpenBankingCategorySeed.Categories)
        {
            if (existingByOpenBankingId.TryGetValue(seed.Id, out var existing))
            {
                existing.Name = seed.DescriptionTranslated;
                existing.Type = OpenBankingCategorySeed.ResolveType(seed.Id);
                existing.Color = OpenBankingCategorySeed.ResolveColor(seed.Id);
                existing.IsDeleted = false;
                existing.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Categories.UpdateAsync(existing);
            }
            else
            {
                await _unitOfWork.Categories.AddAsync(new Category
                {
                    UserId = userId,
                    Name = seed.DescriptionTranslated,
                    Type = OpenBankingCategorySeed.ResolveType(seed.Id),
                    Color = OpenBankingCategorySeed.ResolveColor(seed.Id),
                    OpenBankingCategoryId = seed.Id
                });
            }

            createdCount++;
        }

        user.HasMigratedOpenBankingCategories = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Migração de categorias Open Banking concluída para usuário {UserId}: {DeactivatedCount} categoria(s) antiga(s) desativada(s), {CreatedCount} nova(s) criada(s)",
            userId, deactivatedCount, createdCount);

        // Corrige imediatamente as transações já sincronizadas sem categoria.
        await RecategorizeExistingTransactionsAsync(userId);

        return true;
    }

    public async Task<string> ResolveUserCategoryIdAsync(string userId, string openBankingCategoryId)
    {
        var categories = (await _unitOfWork.Categories.GetByUserIdAsync(userId)).ToList();

        var match = categories.FirstOrDefault(c =>
            !c.IsDeleted && c.OpenBankingCategoryId == openBankingCategoryId);

        if (match is not null)
            return match.Id;

        return await GetOrCreateFallbackCategoryIdAsync(userId, categories);
    }

    public async Task<int> RecategorizeExistingTransactionsAsync(string userId)
    {
        var categories = (await _unitOfWork.Categories.GetByUserIdAsync(userId)).ToList();
        var categoryMap = categories
            .Where(c => !c.IsDeleted && !string.IsNullOrEmpty(c.OpenBankingCategoryId))
            .ToDictionary(c => c.OpenBankingCategoryId!, c => c.Id);

        string? fallbackCategoryId = null;
        var recategorizedCount = 0;

        var transactions = await _unitOfWork.Transactions.GetByUserIdAsync(userId);
        foreach (var transaction in transactions.Where(t =>
            !t.IsDeleted
            && string.IsNullOrEmpty(t.CategoryId)
            && !string.IsNullOrEmpty(t.OpenBankingCategoryId)))
        {
            if (!categoryMap.TryGetValue(transaction.OpenBankingCategoryId!, out var categoryId))
            {
                fallbackCategoryId ??= await GetOrCreateFallbackCategoryIdAsync(userId, categories);
                categoryId = fallbackCategoryId;
            }

            transaction.CategoryId = categoryId;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Transactions.UpdateAsync(transaction);
            recategorizedCount++;
        }

        var creditCardTransactions = await _unitOfWork.CreditCardTransactions.GetByUserAsync(userId);
        foreach (var transaction in creditCardTransactions.Where(t =>
            string.IsNullOrEmpty(t.CategoryId)
            && !string.IsNullOrEmpty(t.OpenBankingCategoryId)))
        {
            if (!categoryMap.TryGetValue(transaction.OpenBankingCategoryId!, out var categoryId))
            {
                fallbackCategoryId ??= await GetOrCreateFallbackCategoryIdAsync(userId, categories);
                categoryId = fallbackCategoryId;
            }

            transaction.CategoryId = categoryId;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CreditCardTransactions.UpdateAsync(transaction);
            recategorizedCount++;
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Recategorização Open Banking para usuário {UserId}: {Count} transação(ões) recategorizada(s)",
            userId, recategorizedCount);

        return recategorizedCount;
    }

    // Busca a categoria fallback "Outros" (OpenBankingCategoryId == 99999999); reativa se estiver
    // soft-deleted (para não violar o índice único parcial) ou cria se não existir.
    private async Task<string> GetOrCreateFallbackCategoryIdAsync(string userId, List<Category> categories)
    {
        var fallback = categories.FirstOrDefault(c =>
            c.OpenBankingCategoryId == OpenBankingCategorySeed.FallbackCategoryId);

        if (fallback is not null)
        {
            if (fallback.IsDeleted)
            {
                fallback.IsDeleted = false;
                fallback.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Categories.UpdateAsync(fallback);
                await _unitOfWork.SaveChangesAsync();
            }

            return fallback.Id;
        }

        var seed = OpenBankingCategorySeed.Categories
            .First(c => c.Id == OpenBankingCategorySeed.FallbackCategoryId);

        var created = new Category
        {
            UserId = userId,
            Name = seed.DescriptionTranslated,
            Type = OpenBankingCategorySeed.ResolveType(seed.Id),
            Color = OpenBankingCategorySeed.ResolveColor(seed.Id),
            OpenBankingCategoryId = seed.Id
        };

        await _unitOfWork.Categories.AddAsync(created);
        await _unitOfWork.SaveChangesAsync();
        categories.Add(created);

        _logger.LogInformation(
            "Categoria fallback \"Outros\" criada para usuário {UserId}", userId);

        return created.Id;
    }
}
