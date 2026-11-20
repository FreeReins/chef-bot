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

    public bool ShouldRedirect(HttpRequest request) =>
        _requireHttps && certificates.IsEnabled && !request.IsHttps && !request.Path.StartsWithSegments("/health");

    /// <summary>The same address over HTTPS, on the port people reach ChefBot's HTTPS on.</summary>
    public string HttpsUrl(HttpRequest request)
    {
        var port = certificates.Options.PublicPort == 443 ? string.Empty : $":{certificates.Options.PublicPort}";
        return $"https://{request.Host.Host}{port}{request.PathBase}{request.Path}{request.QueryString}";
    }
}
