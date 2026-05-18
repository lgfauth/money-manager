using MoneyManager.Domain.Exceptions;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var statusCode = DetermineStatusCode(ex);
            _logger.LogError(ex,
                "Unhandled exception: {ExceptionType} | Path: {Path} | Method: {Method} | StatusCode: {StatusCode} | User: {User}",
                ex.GetType().Name,
                context.Request.Path,
                context.Request.Method,
                statusCode,
                context.User?.Identity?.Name ?? "Anonymous");

            await HandleExceptionAsync(context, ex);
        }
    }

    private static int DetermineStatusCode(Exception exception) => exception switch
    {
        KeyNotFoundException => StatusCodes.Status404NotFound,
        InvalidOperationException => StatusCodes.Status400BadRequest,
        UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
        ConcurrencyException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var statusCode = DetermineStatusCode(exception);

        // Detalhes da InnerException apenas em desenvolvimento para não vazar info interna em produção
        var details = _environment.IsDevelopment()
            ? exception.InnerException?.Message
            : null;

        var response = ApiErrorResponseFactory.Create(
            context,
            statusCode,
            exception.Message,
            details: details);

        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(response);
    }
}
