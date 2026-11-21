namespace ChefBot.Core.Utilities;

internal static class WebAddress
{
    /// <summary>True for an absolute http or https address.</summary>
    public static bool IsHttp(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
