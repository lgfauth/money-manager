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

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new
        {
            message = "AccountDeletion controller is working",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("data-count")]
    [Authorize]
    public async Task<IActionResult> GetDataCount()
    {
        var userId = HttpContext.GetUserId();

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Usuário não autenticado" });

        var count = await _accountDeletionService.GetUserDataCountAsync(userId);

        return Ok(new
        {
            totalRecords = count,
            message = $"Você possui {count} registros que serão permanentemente excluídos."
        });
    }

    [HttpPost("delete-account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequestDto request)
    {
        var userId = HttpContext.GetUserId();

        if (request.ConfirmationText != "DELETAR MINHA CONTA")
            return BadRequest(new { message = "Texto de confirmação incorreto" });

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
}
