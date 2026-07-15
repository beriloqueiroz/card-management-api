using Cards.Api.Auth;
using Cards.Api.ErrorHandling;
using Cards.Application;
using Cards.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cards.UnitTests.Api;

public class GlobalExceptionHandlerTests
{
    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Context { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Context = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Context = context;
            return ValueTask.FromResult(true);
        }
    }

    private static async Task<(int Status, string? Title, string? Detail)> Handle(Exception exception)
    {
        var problemDetails = new CapturingProblemDetailsService();
        var handler = new GlobalExceptionHandler(
            problemDetails, NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        var details = problemDetails.Context!.ProblemDetails;
        return (httpContext.Response.StatusCode, details.Title, details.Detail);
    }

    [Fact]
    public async Task DomainValidation_MapsTo400WithTheDomainMessage()
    {
        var (status, title, detail) = await Handle(
            new DomainValidationException("creditLimit must be greater than or equal to zero."));

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("Validation failed", title);
        Assert.Equal("creditLimit must be greater than or equal to zero.", detail);
    }

    [Fact]
    public async Task CardNotFound_MapsTo404()
    {
        var (status, title, _) = await Handle(new CardNotFoundException());

        Assert.Equal(StatusCodes.Status404NotFound, status);
        Assert.Equal("Not found", title);
    }

    [Fact]
    public async Task UnknownUser_MapsTo403()
    {
        var (status, title, _) = await Handle(new UnknownUserException());

        Assert.Equal(StatusCodes.Status403Forbidden, status);
        Assert.Equal("Forbidden", title);
    }

    [Fact]
    public async Task UnexpectedException_MapsToGeneric500WithoutLeakingInternals()
    {
        var (status, _, detail) = await Handle(
            new InvalidOperationException("connection string contains password=hunter2"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal("An unexpected error occurred.", detail);
        Assert.DoesNotContain("hunter2", detail);
    }
}
