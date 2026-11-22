using ChefBot.Api.Https;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>Applies a changed "always use HTTPS" setting to the running web server.</summary>
[StickyHandler(MessageQueues.Maintenance)]
public static class SecuritySettingsChangedHandler
{
    public static void Handle(SecuritySettingsChanged message, HttpsPolicy policy) => policy.RequireHttps = message.RequireHttps;
}
