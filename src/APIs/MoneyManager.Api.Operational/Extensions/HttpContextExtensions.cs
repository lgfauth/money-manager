using System.Security.Claims;

namespace MoneyManager.Presentation.Extensions;

public static class HttpContextExtensions
{
    public static string GetUserId(this HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    public static string GetUserEmail(this HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }
}
