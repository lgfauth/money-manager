using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountDeletionController : ControllerBase
{
    private readonly IAccountDeletionService _accountDeletionService;

    public AccountDeletionController(IAccountDeletionService accountDeletionService)
    {
        _accountDeletionService = accountDeletionService;
    }

    /// <summary>
    /// Teste de conexão - endpoint público
    /// </summary>
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new 
        { 
            message = "AccountDeletion controller está funcionando!",
            timestamp = DateTime.UtcNow,
            authenticated = User.Identity?.IsAuthenticated ?? false,
            userName = User.Identity?.Name ?? "anonymous"
        });
    }

    /// <summary>
    /// Obtém a contagem de dados do usuário antes da exclusão
    /// </summary>
    [HttpGet("data-count")]
    [Authorize]
    public async Task<IActionResult> GetDataCount()
    {
        try
        {
            Console.WriteLine("[AccountDeletion] Iniciando GetDataCount");
            Console.WriteLine($"[AccountDeletion] User.Identity.IsAuthenticated: {User.Identity?.IsAuthenticated}");
            Console.WriteLine($"[AccountDeletion] User.Claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            
            var userId = HttpContext.GetUserId();
            Console.WriteLine($"[AccountDeletion] UserId obtido: {userId}");
            
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("[AccountDeletion] UserId vazio - não autenticado");
                return Unauthorized(new { message = "Usuário não autenticado" });
            }

            var count = await _accountDeletionService.GetUserDataCountAsync(userId);
            Console.WriteLine($"[AccountDeletion] Contagem retornada: {count}");
            
            return Ok(new 
            { 
                totalRecords = count,
                message = $"Você possui {count} registros que serão permanentemente excluídos."
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AccountDeletion] ERRO: {ex.Message}");
            Console.WriteLine($"[AccountDeletion] StackTrace: {ex.StackTrace}");
            return StatusCode(500, new { message = "Erro ao obter contagem de dados", error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Deleta completamente a conta do usuário e todos os dados relacionados
    /// ATENÇÃO: Esta ação é IRREVERSÍVEL!
    /// </summary>
    [HttpPost("delete-account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequestDto request)
    {
        try
        {
            var userId = HttpContext.GetUserId();

            // Validar texto de confirmação
            if (request.ConfirmationText != "DELETAR MINHA CONTA")
            {
                return BadRequest(new { message = "Texto de confirmação incorreto" });
            }

            // Deletar conta
            var result = await _accountDeletionService.DeleteUserAccountAsync(userId, request.Password);

            if (result)
            {
                return Ok(new 
                { 
                    message = "Conta deletada com sucesso",
                    deleted = true
                });
            }

            return BadRequest(new { message = "Erro ao deletar conta" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Senha incorreta" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao deletar conta", error = ex.Message });
        }
    }
}
