using System.Security.Claims;
using MoneyManager.Observability;

namespace MoneyManager.Presentation.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var processLogger = context.RequestServices.GetRequiredService<IProcessLogger>();

        var request = context.Request;
        processLogger.Start($"{request.Method} {request.Path}", new Dictionary<string, object?>
        {
            ["source"] = "Api",
            ["httpMethod"] = request.Method,
            ["path"] = request.Path.Value,
            ["queryString"] = request.QueryString.HasValue ? request.QueryString.Value : null
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
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
                processLogger.AddContext("userId", userId);

            processLogger.AddContext("statusCode", context.Response.StatusCode);

            var success = caughtException == null && context.Response.StatusCode < 500;
            processLogger.Finish(success, caughtException);
        }
    }
}
