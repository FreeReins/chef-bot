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
