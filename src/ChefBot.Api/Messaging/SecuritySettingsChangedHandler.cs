using ChefBot.Api.Https;
using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Api.Messaging;

/// <summary>Applies a changed "always use HTTPS" setting to the running web server.</summary>
public static class SecuritySettingsChangedHandler
{
    public static void Handle(SecuritySettingsChanged message, HttpsPolicy policy) => policy.RequireHttps = message.RequireHttps;
}
