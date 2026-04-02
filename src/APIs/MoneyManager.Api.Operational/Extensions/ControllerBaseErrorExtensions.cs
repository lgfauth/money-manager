using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace MoneyManager.Presentation.Extensions;

public static class ControllerBaseErrorExtensions
{
    public static ObjectResult ApiError(
        this ControllerBase controller,
        int statusCode,
        string message,
        IEnumerable<string>? errors = null,
        string? details = null)
    {
        var response = ApiErrorResponseFactory.Create(
            controller.HttpContext,
            statusCode,
            message,
            errors,
            details);

        return new ObjectResult(response)
        {
            StatusCode = statusCode
        };
    }

    public static BadRequestObjectResult ApiBadRequest(
        this ControllerBase controller,
        string message,
        IEnumerable<string>? errors = null,
        string? details = null) =>
        new(ApiErrorResponseFactory.Create(
            controller.HttpContext,
            StatusCodes.Status400BadRequest,
            message,
            errors,
            details));

    public static BadRequestObjectResult ApiValidationError(
        this ControllerBase controller,
        IEnumerable<ValidationFailure> validationFailures,
        string message = "Validation failed") =>
        new(ApiErrorResponseFactory.CreateValidation(
            controller.HttpContext,
            validationFailures,
            message));

    public static UnauthorizedObjectResult ApiUnauthorized(
        this ControllerBase controller,
        string message = "Unauthorized",
        IEnumerable<string>? errors = null) =>
        new(ApiErrorResponseFactory.Create(
            controller.HttpContext,
            StatusCodes.Status401Unauthorized,
            message,
            errors));

    public static NotFoundObjectResult ApiNotFound(
        this ControllerBase controller,
        string message = "Resource not found",
        IEnumerable<string>? errors = null) =>
        new(ApiErrorResponseFactory.Create(
            controller.HttpContext,
            StatusCodes.Status404NotFound,
            message,
            errors));

    public static ObjectResult ApiConflict(
        this ControllerBase controller,
        string message,
        IEnumerable<string>? errors = null) =>
        controller.ApiError(StatusCodes.Status409Conflict, message, errors);

    public static ObjectResult ApiServerError(
        this ControllerBase controller,
        string message = "Internal server error",
        IEnumerable<string>? errors = null,
        string? details = null) =>
        controller.ApiError(StatusCodes.Status500InternalServerError, message, errors, details);
}