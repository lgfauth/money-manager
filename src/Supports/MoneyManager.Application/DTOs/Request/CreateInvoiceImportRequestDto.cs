namespace MoneyManager.Application.DTOs.Request;

/// <summary>
/// Upload de uma fatura para importação: um PDF ou de 1 a 10 imagens, nunca misturados.
/// O controller converte o multipart neste DTO, que não depende do ASP.NET.
/// </summary>
public class CreateInvoiceImportRequestDto
{
    public string CreditCardId { get; set; } = string.Empty;
    public List<InvoiceImportUploadFileRequestDto> Files { get; set; } = [];
}

public class InvoiceImportUploadFileRequestDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];

    public long SizeBytes => Content.LongLength;
}
