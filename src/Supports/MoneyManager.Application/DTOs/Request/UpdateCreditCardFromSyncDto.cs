namespace MoneyManager.Application.DTOs.Request;

public class UpdateCreditCardFromSyncDto
{
    public decimal? Limit { get; set; }
    public decimal? AvailableLimit { get; set; }
    public string? Brand { get; set; }
    public int? DueDay { get; set; }
    public int? ClosingDay { get; set; }
}
