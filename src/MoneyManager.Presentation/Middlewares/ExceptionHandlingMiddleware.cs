namespace MoneyManager.Presentation.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        _ => StatusCodes.Status500InternalServerError
    };

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = exception.Message,
            details = exception.InnerException?.Message
        };

        context.Response.StatusCode = DetermineStatusCode(exception);

        return context.Response.WriteAsJsonAsync(response);
    }
}
