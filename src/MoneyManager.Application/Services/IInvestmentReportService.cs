using MoneyManager.Application.DTOs.Response;

namespace MoneyManager.Application.Services;

/// <summary>
/// Serviço para geração de relatórios de investimentos.
/// Suporta relatórios para análise e declaração de IR.
/// </summary>
public interface IInvestmentReportService
{
    /// <summary>
    /// Gera relatório de vendas de investimentos para declaração de IR.
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="year">Ano de referência</param>
    /// <returns>Relatório de vendas</returns>
    Task<InvestmentSalesReportDto> GenerateSalesReportAsync(string userId, int year);

    /// <summary>
    /// Gera relatório de rendimentos recebidos (dividendos, juros, aluguéis).
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="year">Ano de referência</param>
    /// <returns>Relatório de rendimentos</returns>
    Task<InvestmentYieldsReportDto> GenerateYieldsReportAsync(string userId, int year);

    /// <summary>
    /// Gera extrato consolidado de todas as transações de investimento.
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="startDate">Data inicial</param>
    /// <param name="endDate">Data final</param>
    /// <returns>Extrato consolidado</returns>
    Task<InvestmentConsolidatedStatementDto> GenerateConsolidatedStatementAsync(
        string userId, 
        DateTime startDate, 
        DateTime endDate);

    /// <summary>
    /// Exporta relatório de vendas para PDF.
    /// </summary>
    /// <param name="report">Dados do relatório</param>
    /// <returns>Bytes do arquivo PDF</returns>
    Task<byte[]> ExportSalesReportToPdfAsync(InvestmentSalesReportDto report);

    /// <summary>
    /// Exporta relatório de vendas para Excel.
    /// </summary>
    /// <param name="report">Dados do relatório</param>
    /// <returns>Bytes do arquivo Excel</returns>
    Task<byte[]> ExportSalesReportToExcelAsync(InvestmentSalesReportDto report);

    /// <summary>
    /// Exporta relatório de rendimentos para Excel.
    /// </summary>
    /// <param name="report">Dados do relatório</param>
    /// <returns>Bytes do arquivo Excel</returns>
    Task<byte[]> ExportYieldsReportToExcelAsync(InvestmentYieldsReportDto report);
}
