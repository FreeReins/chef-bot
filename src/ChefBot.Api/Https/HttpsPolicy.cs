namespace ChefBot.Api.Https;

/// <summary>
/// Decides whether a plain HTTP request should be sent to HTTPS. The "always use HTTPS" switch is an
/// admin setting kept in the database; it is held here so each request doesn't have to read it.
/// </summary>
public sealed class HttpsPolicy(CertificateStore certificates)
{
    private volatile bool _requireHttps;

    /// <summary>The admin's "always use HTTPS" setting.</summary>
    public bool RequireHttps
    {
        get => _requireHttps;
        set => _requireHttps = value;
    }

    /// <summary>A year, the usual length. Browsers remember it from the last HTTPS visit.</summary>
    public const string HstsHeader = "max-age=31536000";

    public bool ShouldRedirect(HttpRequest request) =>
        _requireHttps && certificates.IsEnabled && !request.IsHttps && !request.Path.StartsWithSegments("/health");

    /// <summary>
    /// True when browsers should be told to use only HTTPS for this site from now on. Only with a certificate you
    /// provide: a browser that has seen HSTS won't let anyone click past a certificate warning, and a self-signed
    /// certificate shows one until each device trusts it. Behind a proxy (Https:Mode Off), the proxy sends HSTS.
    /// </summary>
    public bool ShouldSendHsts(HttpRequest request) =>
        _requireHttps && certificates.Options.Mode == HttpsMode.Certificate && request.IsHttps;

    /// <summary>The same address over HTTPS, on the port people reach ChefBot's HTTPS on.</summary>
    public string HttpsUrl(HttpRequest request)
    {
        var port = certificates.Options.PublicPort == 443 ? string.Empty : $":{certificates.Options.PublicPort}";
        return $"https://{request.Host.Host}{port}{request.PathBase}{request.Path}{request.QueryString}";
    }
}
