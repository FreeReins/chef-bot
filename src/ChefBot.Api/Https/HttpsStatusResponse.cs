namespace ChefBot.Api.Https;

/// <summary>How ChefBot is serving HTTPS right now.</summary>
/// <param name="Mode">Off, SelfSigned or Certificate.</param>
/// <param name="Enabled">True when an HTTPS address exists.</param>
/// <param name="Port">The HTTPS port to use in the browser.</param>
/// <param name="Hostnames">The names a self-signed certificate covers.</param>
/// <param name="ExpiresOn">When the certificate expires. Self-signed ones renew themselves at startup.</param>
/// <param name="CanRegenerate">True when ChefBot owns the certificate and can create a new one.</param>
/// <param name="IsSecureNow">True when this request arrived over HTTPS.</param>
/// <param name="RequireHttps">True when plain HTTP visitors are sent to HTTPS.</param>
public sealed record HttpsStatusResponse(
    HttpsMode Mode,
    bool Enabled,
    int Port,
    IReadOnlyList<string> Hostnames,
    DateTimeOffset? ExpiresOn,
    bool CanRegenerate,
    bool IsSecureNow,
    bool RequireHttps);
