using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountDeletionController : ControllerBase
{
    private readonly IAccountDeletionService _accountDeletionService;

    public AccountDeletionController(IAccountDeletionService accountDeletionService)
    {
        _accountDeletionService = accountDeletionService;
    }

    /// <summary>
    /// Obtém a contagem de dados do usuário antes da exclusão
    /// </summary>
    [HttpGet("data-count")]
    public async Task<IActionResult> GetDataCount()
    {
        var userId = HttpContext.GetUserId();
        var count = await _accountDeletionService.GetUserDataCountAsync(userId);
        
        return Ok(new 
        { 
            totalRecords = count,
            message = $"Você possui {count} registros que serão permanentemente excluídos."
        });
    }

    /// <summary>
    /// Deleta completamente a conta do usuário e todos os dados relacionados
    /// ATENÇÃO: Esta ação é IRREVERSÍVEL!
    /// </summary>
    [HttpPost("delete-account")]
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
