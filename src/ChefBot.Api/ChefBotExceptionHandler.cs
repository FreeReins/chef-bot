using ChefBot.Core.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ChefBot.Api;

/// <summary>Turns ChefBot's own exceptions into problem responses with a readable message.</summary>
internal sealed class ChefBotExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            RuleViolationException => (StatusCodes.Status400BadRequest, "Invalid request"),
            RecipeSourceException => (StatusCodes.Status502BadGateway, "Recipe source unavailable"),
            NotSignedInException => (StatusCodes.Status401Unauthorized, "Not signed in"),
            NotAllowedException => (StatusCodes.Status403Forbidden, "Not allowed"),
            _ => (0, string.Empty),
        };

        if (status == 0)
        {
            return false; // Anything unexpected falls through to the default 500 response.
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message },
        });
    }
}
