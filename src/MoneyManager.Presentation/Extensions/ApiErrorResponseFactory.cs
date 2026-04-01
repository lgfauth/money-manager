using FluentValidation.Results;
using MoneyManager.Presentation.Models;

namespace MoneyManager.Presentation.Extensions;

public static class ApiErrorResponseFactory
{
    public static ApiErrorResponse Create(
        HttpContext context,
        int statusCode,
        string message,
        IEnumerable<string>? errors = null,
        string? details = null)
    {
        var normalizedMessage = string.IsNullOrWhiteSpace(message)
            ? GetDefaultMessage(statusCode)
            : message;

        var errorList = (errors ?? [])
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct()
            .ToList();

        return new ApiErrorResponse
        {
            StatusCode = statusCode,
            Message = normalizedMessage,
            Errors = errorList,
            TraceId = context.TraceIdentifier,
            Path = context.Request.Path.Value ?? string.Empty,
            TimestampUtc = DateTime.UtcNow,
            Details = string.IsNullOrWhiteSpace(details) ? null : details
        };
    }

    public static ApiErrorResponse CreateValidation(
        HttpContext context,
        IEnumerable<ValidationFailure> validationFailures,
        string message = "Validation failed")
    {
        var failures = validationFailures.ToList();

        return Create(
            context,
            StatusCodes.Status400BadRequest,
            message,
            failures.Select(failure => failure.ErrorMessage));
    }

    private static string GetDefaultMessage(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Resource not found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Internal server error"
    };
}