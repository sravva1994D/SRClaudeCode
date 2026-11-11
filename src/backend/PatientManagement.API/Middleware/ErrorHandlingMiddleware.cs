using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PatientManagement.API.Errors;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.API.Middleware;

/// <summary>
/// Converts exceptions into the <see cref="ErrorResponse"/> shape. Domain failures become 422 with their own code;
/// anything else becomes a generic 500 and is logged without its message, which can contain data values.
/// </summary>
public sealed partial class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public const string UnexpectedErrorCode = "internal_error";
    public const string UnexpectedErrorMessage =
        "An unexpected error occurred. Quote the correlation id when you report it.";

    public const string ValidationFailedCode = "validation_failed";
    public const string ValidationFailedMessage = "One or more fields are invalid.";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException)
        {
            // Field names and values are not echoed: they can contain patient or credential data.
            LogRejectedRequest(logger, ValidationFailedCode, CorrelationIdOf(context));
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ValidationFailedCode, ValidationFailedMessage);
        }
        catch (AuthenticationFailedException exception)
        {
            LogRejectedRequest(logger, exception.ErrorCode, CorrelationIdOf(context));
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, exception.ErrorCode, exception.Message);
        }
        catch (ConcurrencyConflictException exception)
        {
            LogRejectedRequest(logger, exception.ErrorCode, CorrelationIdOf(context));
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, exception.ErrorCode, exception.Message);
        }
        catch (DomainException exception)
        {
            LogRejectedRequest(logger, exception.ErrorCode, CorrelationIdOf(context));
            await WriteErrorAsync(context, StatusCodes.Status422UnprocessableEntity, exception.ErrorCode, exception.Message);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            LogUnexpectedError(logger, CorrelationIdOf(context), exception.GetType().FullName ?? "unknown", exception.StackTrace ?? string.Empty);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, UnexpectedErrorCode, UnexpectedErrorMessage);
        }
    }

    private static string CorrelationIdOf(HttpContext context) =>
        context.Items[CorrelationIdMiddleware.ItemKey] as string ?? string.Empty;

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
    {
        // No Clear(): it would also remove the correlation id header that was set before the failure.
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            new ErrorResponse(code, message, CorrelationIdOf(context)),
            context.RequestAborted);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request {CorrelationId} rejected: {ErrorCode}")]
    private static partial void LogRejectedRequest(ILogger logger, string errorCode, string correlationId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request {CorrelationId} failed ({ExceptionType}). Stack: {StackTrace}")]
    private static partial void LogUnexpectedError(ILogger logger, string correlationId, string exceptionType, string stackTrace);
}
