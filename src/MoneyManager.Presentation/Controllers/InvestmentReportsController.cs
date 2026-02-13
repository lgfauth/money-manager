using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

/// <summary>
/// Controller para relatórios de investimentos.
/// </summary>
[Authorize]
[ApiController]
[Route("api/investment-reports")]
public class InvestmentReportsController : ControllerBase
{
    private readonly IInvestmentReportService _reportService;
    private readonly ILogger<InvestmentReportsController> _logger;

    public InvestmentReportsController(
        IInvestmentReportService reportService,
        ILogger<InvestmentReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Gera relatório de vendas para declaração de IR.
    /// </summary>
    /// <param name="year">Ano de referência</param>
    /// <returns>Relatório de vendas em JSON</returns>
    [HttpGet("sales/{year}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSalesReport([FromRoute] int year)
    {
        var userId = HttpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (year < 2000 || year > DateTime.Now.Year)
        {
            return BadRequest("Ano inválido");
        }

        _logger.LogInformation("Usuário {UserId} solicitou relatório de vendas do ano {Year}", userId, year);

        var report = await _reportService.GenerateSalesReportAsync(userId, year);
        return Ok(report);
    }

    /// <summary>
    /// Gera relatório de rendimentos recebidos.
    /// </summary>
    /// <param name="year">Ano de referência</param>
    /// <returns>Relatório de rendimentos em JSON</returns>
    [HttpGet("yields/{year}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetYieldsReport([FromRoute] int year)
    {
        var userId = HttpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (year < 2000 || year > DateTime.Now.Year)
        {
            return BadRequest("Ano inválido");
        }

        _logger.LogInformation("Usuário {UserId} solicitou relatório de rendimentos do ano {Year}", userId, year);

        var report = await _reportService.GenerateYieldsReportAsync(userId, year);
        return Ok(report);
    }

    /// <summary>
    /// Gera extrato consolidado de investimentos.
    /// </summary>
    /// <param name="start">Data inicial (formato: yyyy-MM-dd)</param>
    /// <param name="end">Data final (formato: yyyy-MM-dd)</param>
    /// <returns>Extrato consolidado em JSON</returns>
    [HttpGet("consolidated")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConsolidatedStatement(
        [FromQuery] string start,
        [FromQuery] string end)
    {
        var userId = HttpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (!DateTime.TryParse(start, out var startDate))
        {
            return BadRequest("Data inicial inválida");
        }

        if (!DateTime.TryParse(end, out var endDate))
        {
            return BadRequest("Data final inválida");
        }

        if (startDate > endDate)
        {
            return BadRequest("Data inicial deve ser anterior à data final");
        }

        _logger.LogInformation(
            "Usuário {UserId} solicitou extrato consolidado de {Start} a {End}",
            userId, startDate, endDate);

        var statement = await _reportService.GenerateConsolidatedStatementAsync(userId, startDate, endDate);
        return Ok(statement);
    }

    /// <summary>
    /// Exporta relatório de vendas para PDF.
    /// </summary>
    /// <param name="year">Ano de referência</param>
    /// <returns>Arquivo PDF</returns>
    [HttpGet("sales/{year}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> ExportSalesReportToPdf([FromRoute] int year)
    {
        var userId = HttpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (year < 2000 || year > DateTime.Now.Year)
        {
            return BadRequest("Ano inválido");
        }

        try
        {
            var report = await _reportService.GenerateSalesReportAsync(userId, year);
            var pdfBytes = await _reportService.ExportSalesReportToPdfAsync(report);

            var fileName = $"vendas-investimentos-{year}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, 
                "Exportação para PDF ainda não implementada");
        }
    }

    /// <summary>
    /// Exporta relatório de vendas para Excel.
    /// </summary>
    /// <param name="year">Ano de referência</param>
    /// <returns>Arquivo Excel</returns>
    [HttpGet("sales/{year}/excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> ExportSalesReportToExcel([FromRoute] int year)
    {
        var userId = HttpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (year < 2000 || year > DateTime.Now.Year)
        {
            return BadRequest("Ano inválido");
        }

        try
        {
            var report = await _reportService.GenerateSalesReportAsync(userId, year);
            var excelBytes = await _reportService.ExportSalesReportToExcelAsync(report);

            var fileName = $"vendas-investimentos-{year}.xlsx";
            return File(excelBytes, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, 
                "Exportação para Excel ainda não implementada");
        }
    }

    /// <summary>
    /// Exporta relatório de rendimentos para Excel.
    /// </summary>
    /// <param name="year">Ano de referência</param>
    /// <returns>Arquivo Excel</returns>
    [HttpGet("yields/{year}/excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> ExportYieldsReportToExcel([FromRoute] int year)
    {
        var userId = HttpContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (year < 2000 || year > DateTime.Now.Year)
        {
            return BadRequest("Ano inválido");
        }

        try
        {
            var report = await _reportService.GenerateYieldsReportAsync(userId, year);
            var excelBytes = await _reportService.ExportYieldsReportToExcelAsync(report);

            var fileName = $"rendimentos-investimentos-{year}.xlsx";
            return File(excelBytes, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }
        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, 
                "Exportação para Excel ainda não implementada");
        }
    }
}
