namespace ChefBot.Core.Utilities;

/// <summary>Tells which kind of image a file is from its first bytes, for the kinds a browser shows everywhere.</summary>
internal static class ImageFormat
{
    /// <summary>The media type, such as image/png, or null when the bytes aren't a JPEG, PNG, WebP or GIF image.</summary>
    public static string? Detect(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])) return "image/jpeg";
        if (data.StartsWith((ReadOnlySpan<byte>)[0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A])) return "image/png";
        if (data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8)) return "image/gif";
        if (data.Length >= 12 && data.StartsWith("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }
}
