using Cards.Api.Auth;
using Cards.Application;
using Cards.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cards.Api.ErrorHandling;

/// <summary>
/// Maps exceptions to RFC 7807 ProblemDetails with coherent status codes.
/// Unexpected errors return a generic 500 — internals and sensitive values
/// never leak into responses.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var (status, title, detail) = exception switch
        {
            DomainValidationException e => (StatusCodes.Status400BadRequest, "Validation failed", e.Message),
            CardNotFoundException e => (StatusCodes.Status404NotFound, "Not found", e.Message),
            UnknownUserException e => (StatusCodes.Status403Forbidden, "Forbidden", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "An unexpected error occurred."),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
            },
        });
    }
}
