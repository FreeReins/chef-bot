using System.Text.Encodings.Web;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ChefBot.Api;

/// <summary>
/// Signs an assistant in with an API token sent as <c>Authorization: Bearer {token}</c>, as the account that made
/// the token. Only the MCP endpoint accepts it (<see cref="Sessions.McpPolicy"/>); the rest of the API uses the
/// sign-in cookie.
/// </summary>
public sealed class ApiTokenAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The name of the authentication scheme.</summary>
    public const string SchemeName = "ApiToken";

    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var accounts = Context.RequestServices.GetRequiredService<IAccountManager>();
        var session = await accounts.FindApiTokenSessionAsync(header[BearerPrefix.Length..].Trim(), Context.RequestAborted);
        return session is null
            ? AuthenticateResult.Fail("That API token doesn't work. Make a new one under Settings, AI assistants.")
            : AuthenticateResult.Success(new AuthenticationTicket(Sessions.Principal(session, SchemeName), SchemeName));
    }
}
