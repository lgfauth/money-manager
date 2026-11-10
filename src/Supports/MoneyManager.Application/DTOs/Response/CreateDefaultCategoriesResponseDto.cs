namespace MoneyManager.Application.DTOs.Response;

/// <summary>Resultado da criação das categorias padrão: quantas foram criadas ou reativadas e as categorias ativas do usuário.</summary>
public class CreateDefaultCategoriesResponseDto
{
    public int CreatedCount { get; set; }
    public List<CategoryResponseDto> Categories { get; set; } = [];
}
