namespace ChefBot.Core.Utilities;

internal static class WebAddress
{
    /// <summary>True for an absolute http or https address.</summary>
    public static bool IsHttp(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>True when both are absolute addresses on the same scheme, host and port.</summary>
    public static bool SameServer(string? first, string? second) =>
        Uri.TryCreate(first, UriKind.Absolute, out var a) && Uri.TryCreate(second, UriKind.Absolute, out var b)
        && Uri.Compare(a, b, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;
}
