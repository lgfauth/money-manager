using System.Security.Claims;
using MoneyManager.Observability;

namespace MoneyManager.Api.Administration.Middlewares;

// Mesmo padrão do RequestLoggingMiddleware da API Operational — gera JSON estruturado
// por request via IProcessLogger, com identificação do operador admin.
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        // Health checks e raiz não passam pelo log — evitam ruído no store de process logs.
        if (request.Path == "/" || request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var processLogger = context.RequestServices.GetRequiredService<IProcessLogger>();

        processLogger.Start($"{request.Method} {request.Path}", new Dictionary<string, object?>
        {
            ["source"] = "AdminApi",
            ["httpMethod"] = request.Method,
            ["path"] = request.Path.Value,
            ["queryString"] = request.QueryString.HasValue ? request.QueryString.Value : null,
            ["remoteIp"] = context.Connection.RemoteIpAddress?.ToString()
        });

        Exception? caughtException = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            caughtException = ex;
            throw;
        }
        finally
        {
            var adminUsername = context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(adminUsername))
                processLogger.AddContext("adminUsername", adminUsername);

            var adminRole = context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(adminRole))
                processLogger.AddContext("adminRole", adminRole);

            processLogger.AddContext("statusCode", context.Response.StatusCode);

            var success = caughtException == null && context.Response.StatusCode < 500;
            processLogger.Finish(success, caughtException);
        }
    }
}
