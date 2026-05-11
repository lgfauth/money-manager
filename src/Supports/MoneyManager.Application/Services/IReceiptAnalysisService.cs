using MoneyManager.Application.DTOs.Response;

namespace MoneyManager.Application.Services;

public interface IReceiptAnalysisService
{
    Task<ReceiptAnalysisResultDto> AnalyzeAsync(string fileBase64, string mimeType);
}
