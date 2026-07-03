namespace MoneyManager.Application.DTOs.Request;

public class UpdateOpenInvoiceFromSyncDto
{
    public decimal TotalAmount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? CloseDate { get; set; }
}
