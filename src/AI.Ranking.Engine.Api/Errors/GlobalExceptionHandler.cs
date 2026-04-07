using AI.Ranking.Engine.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AI.Ranking.Engine.Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            DocumentParseException => (StatusCodes.Status400BadRequest, "Document parsing failed"),
            ExternalServiceException => (StatusCodes.Status503ServiceUnavailable, "External dependency failed"),
            DomainException => (StatusCodes.Status409Conflict, "Domain constraint violation"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (StatusCodes.Status500InternalServerError, "Unhandled server error"),
        };

        if (status >= 500)
            _logger.LogError(exception, "Request failed with status {StatusCode}.", status);
        else
            _logger.LogWarning(exception, "Request failed with status {StatusCode}.", status);

        var detail = status >= 500
            ? "An unexpected error occurred while processing the request."
            : exception.Message;

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status,
            Type = $"https://httpstatuses.com/{status}",
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
