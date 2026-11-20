using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace ChefBot.Api;

/// <summary>
/// Answers a refused request with a readable problem response, as ChefBot's own errors are, instead of an empty
/// 401 or 403, so the web page can show why.
/// </summary>
internal sealed class AdminOnlyHandler(IProblemDetailsService problemDetails) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (result.Succeeded)
        {
            await _default.HandleAsync(next, context, policy, result);
            return;
        }

        var (status, title, detail) = result.Forbidden
            ? (StatusCodes.Status403Forbidden, "Not allowed", "Only an admin can do that.")
            : (StatusCodes.Status401Unauthorized, "Not signed in", "Sign in to continue.");

        context.Response.StatusCode = status;
        await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        });
    }
}
