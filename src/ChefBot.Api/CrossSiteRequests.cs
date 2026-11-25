using Microsoft.AspNetCore.Mvc;

namespace ChefBot.Api;

/// <summary>
/// Refuses requests that change something (anything but GET, HEAD and OPTIONS) when a browser says another web
/// address sent them. The sign-in cookie is SameSite=Strict, which stops other sites, but not another page on the same
/// site, such as a different app on a neighbouring subdomain of a home network; this stops those too.
/// Requests from outside a browser, such as scripts, send neither header and are let through: they can't use
/// someone else's sign-in cookie. The MCP endpoint signs in with a token, not the cookie, so it is left alone.
/// </summary>
public static class CrossSiteRequests
{
    /// <summary>What a refused request is told.</summary>
    public const string Refusal = "ChefBot only accepts changes sent from its own page. Open ChefBot at its own address and try again.";

    /// <summary>
    /// True when the request changes something and the browser says it came from somewhere else:
    /// <c>Sec-Fetch-Site</c> is anything but <c>same-origin</c> (or <c>none</c>, typed by the person themselves), or,
    /// from an older browser that doesn't send it, <c>Origin</c> names a different host than the request went to.
    /// </summary>
    public static bool IsRefused(string method, string path, string? fetchSite, string? origin, string host)
    {
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method)
            || path.StartsWith("/mcp", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(fetchSite))
        {
            return fetchSite is not ("same-origin" or "none");
        }

        if (string.IsNullOrEmpty(origin))
        {
            return false;
        }

        return !Uri.TryCreate(origin, UriKind.Absolute, out var from)
            || !string.Equals(from.IsDefaultPort ? from.Host : from.Authority, host, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Answers a refused request with 403 and a readable problem, before anything else sees it.</summary>
    public static void UseChefBotCrossSiteCheck(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var request = context.Request;
            if (IsRefused(request.Method, request.Path, request.Headers["Sec-Fetch-Site"], request.Headers.Origin, request.Host.Value ?? string.Empty))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = new ProblemDetails { Status = StatusCodes.Status403Forbidden, Title = "Not allowed", Detail = Refusal },
                });
                return;
            }

            await next(context);
        });
    }
}
