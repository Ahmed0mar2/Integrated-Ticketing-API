namespace GP.API.Middleware;

using GP.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Exception occurred: {Message}",
            exception.Message);

        var statusCode = StatusCodes.Status500InternalServerError;
        var message = "An unexpected error occurred.";
        string? errorCode = null;

        switch (exception)
        {
            case BadHttpRequestException badRequestException:
                statusCode = StatusCodes.Status400BadRequest;
                message = badRequestException.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                message = "You are not authorized to access this resource";
                break;

            case CartValidationException cartValidationException:
                statusCode = StatusCodes.Status400BadRequest;
                message = cartValidationException.Message;
                errorCode = cartValidationException.ErrorCode;
                break;

            case CartConcurrencyException cartConcurrencyException:
                statusCode = StatusCodes.Status409Conflict;
                message = cartConcurrencyException.Message;
                errorCode = cartConcurrencyException.ErrorCode;
                break;

            default:
                message = exception.Message;
                break;
        }

        httpContext.Response.StatusCode = statusCode;

        var response = ApiResponse.ErrorResponse(message, errorCode: errorCode);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}