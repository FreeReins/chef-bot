using System.Text.Json.Serialization;

namespace ChefBot.Api.Https;

/// <summary>How ChefBot serves HTTPS.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<HttpsMode>))]
public enum HttpsMode
{
    /// <summary>HTTP only, for example behind a reverse proxy that adds HTTPS itself.</summary>
    Off,

    /// <summary>ChefBot creates and renews its own certificate. Browsers warn until it is trusted.</summary>
    SelfSigned,

    /// <summary>Use a certificate you provide.</summary>
    Certificate,
}

/// <summary>Network settings, bound from the "Https" section. Changing them needs a restart.</summary>
public sealed class HttpsOptions
{
    public HttpsMode Mode { get; set; } = HttpsMode.Off;

    /// <summary>The port ChefBot listens on for plain HTTP.</summary>
    public int HttpPort { get; set; } = 8080;

    /// <summary>The port ChefBot listens on for HTTPS.</summary>
    public int Port { get; set; } = 8443;

    /// <summary>The HTTPS port people type in the browser, when a container maps it to a different one.</summary>
    public int PublicPort { get; set; } = 8443;

    /// <summary>
    /// Self-signed mode: the names and addresses people use to reach ChefBot, separated by commas,
    /// such as "chefbot.local,192.168.1.20". Used for the first certificate; admins can change it later in the app.
    /// </summary>
    public string Hostnames { get; set; } = string.Empty;

    /// <summary>Certificate mode: a .pfx file, or a PEM certificate used together with <see cref="KeyPath"/>.</summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>Certificate mode: the PEM private key, when the certificate is a PEM file.</summary>
    public string KeyPath { get; set; } = string.Empty;

    /// <summary>Certificate mode: the password of the .pfx file, if it has one.</summary>
    public string CertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// Reverse proxies whose X-Forwarded-For and X-Forwarded-Proto headers are believed, separated by commas: IP
    /// addresses such as "10.0.0.5" or networks such as "172.16.0.0/12". Empty, the default, believes no one.
    /// </summary>
    public string TrustedProxies { get; set; } = string.Empty;
}
