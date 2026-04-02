namespace MoneyManager.Api.Administration.Models;

public sealed class UpdateLegalDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public sealed class PreviewLegalDocumentRequest
{
    public string? Content { get; set; }
}
