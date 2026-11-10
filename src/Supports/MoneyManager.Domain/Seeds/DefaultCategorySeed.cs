using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Seeds;

// Categoria da lista padrão oferecida ao usuário ("Criar categorias padrão").
public record DefaultCategoryDefinition(string Name, CategoryType Type, string Color);

// Lista padrão de categorias criada sob demanda pelo usuário (página de categorias e revisão
// da importação de fatura). Também serve de lista de sugestão para a leitura da fatura por IA
// quando o usuário ainda não tem categorias de despesa.
public static class DefaultCategorySeed
{
    public static readonly IReadOnlyList<DefaultCategoryDefinition> Categories =
    [
        // Despesas
        new("Alimentação", CategoryType.Expense, "#E74C3C"),
        new("Supermercado", CategoryType.Expense, "#2ECC71"),
        new("Transporte & Mobilidade", CategoryType.Expense, "#3498DB"),
        new("Saúde & Farmácia", CategoryType.Expense, "#1ABC9C"),
        new("Lazer & Entretenimento", CategoryType.Expense, "#9B59B6"),
        new("Compras & Varejo", CategoryType.Expense, "#D35400"),
        new("Vestuário", CategoryType.Expense, "#E84393"),
        new("Casa", CategoryType.Expense, "#E67E22"),
        new("Contas & Utilidades", CategoryType.Expense, "#7F8C8D"),
        new("Serviços", CategoryType.Expense, "#8E44AD"),
        new("Assinaturas", CategoryType.Expense, "#6C5CE7"),
        new("Educação", CategoryType.Expense, "#2980B9"),
        new("Viagem", CategoryType.Expense, "#16A085"),
        new("Taxas & Cobranças", CategoryType.Expense, "#C0392B"),
        new("Transferência", CategoryType.Expense, "#34495E"),
        new("Investimentos", CategoryType.Expense, "#00B894"),
        new("Crédito & Empréstimo", CategoryType.Expense, "#B33771"),
        new("Outros", CategoryType.Expense, "#95A5A6"),

        // Receitas
        new("Renda & Recebimentos", CategoryType.Income, "#27AE60"),
        new("Crédito & Empréstimo", CategoryType.Income, "#00C896"),
    ];
}
