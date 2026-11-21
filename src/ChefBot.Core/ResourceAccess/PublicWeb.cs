using System.Net;
using System.Net.Sockets;
using ChefBot.Core.Managers.Contracts;

namespace ChefBot.Core.ResourceAccess;

/// <summary>
/// The HTTP handler for downloading from addresses people or web pages choose: recipe pages and recipe photos.
/// It only connects to public internet addresses, so a link can't make ChefBot read its own database, the
/// home network or a cloud metadata service. The address is checked when connecting, after the name is looked
/// up and again after each redirect, so a name that points at a private address is refused too.
/// </summary>
internal static class PublicWeb
{
    public static SocketsHttpHandler Handler() => new()
    {
        // A proxy would make the connection, so the address check would only ever see the proxy's.
        UseProxy = false,
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 3,
        AutomaticDecompression = DecompressionMethods.All,
        ConnectCallback = ConnectAsync,
    };

    /// <summary>
    /// False for loopback, private, link-local, carrier-grade NAT, multicast and reserved addresses, IPv4 or IPv6.
    /// An IPv6 address that carries an IPv4 one (NAT64, 6to4, IPv4-compatible or -translated) is judged by the IPv4
    /// address it leads to, because networks that translate between the two connect to that address.
    /// </summary>
    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return false;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (EmbeddedIPv4(address) is { } inner) return IsPublic(inner);

            var v6 = address.GetAddressBytes();
            return !(address.Equals(IPAddress.IPv6Any) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal
                || address.IsIPv6UniqueLocal || address.IsIPv6Multicast
                || IsUnder(v6, [0x00, 0x64, 0xff, 0x9b, 0x00, 0x01], 48) // NAT64 for local use: translates to anything
                || IsUnder(v6, [0x20, 0x01, 0x00, 0x00], 32)           // Teredo tunnels
                || IsUnder(v6, [0x20, 0x01, 0x0d, 0xb8], 32)           // documentation
                || IsUnder(v6, [0x01, 0x00, 0, 0, 0, 0, 0, 0], 64));   // discard-only
        }

        var b = address.GetAddressBytes();
        return !(b[0] is 0 or 10 or 127 or >= 224          // "this network", private, loopback, multicast and reserved
            || (b[0] == 169 && b[1] == 254)                 // link-local, including cloud metadata services
            || (b[0] == 172 && b[1] is >= 16 and <= 31)     // private
            || (b[0] == 192 && b[1] == 168)                 // private
            || (b[0] == 100 && b[1] is >= 64 and <= 127)    // carrier-grade NAT
            || (b[0] == 192 && b[1] == 0 && b[2] == 0)      // protocol assignments
            || (b[0] == 198 && b[1] is 18 or 19));          // benchmarking
    }

    /// <summary>
    /// The IPv4 address an IPv6 one leads to, or null when it carries none: NAT64 (64:ff9b::/96), 6to4
    /// (2002::/16), IPv4-translated (::ffff:0:0/96) and the old IPv4-compatible form (::/96, apart from :: and ::1).
    /// </summary>
    private static IPAddress? EmbeddedIPv4(IPAddress address)
    {
        var b = address.GetAddressBytes();
        if (IsUnder(b, [0x00, 0x64, 0xff, 0x9b, 0, 0, 0, 0, 0, 0, 0, 0], 96)
            || IsUnder(b, [0, 0, 0, 0, 0, 0, 0, 0, 0xff, 0xff, 0, 0], 96))
        {
            return new IPAddress(b.AsSpan(12, 4));
        }

        if (IsUnder(b, [0x20, 0x02], 16)) return new IPAddress(b.AsSpan(2, 4));

        // ::/96 holds :: and ::1 as well, which are refused as they are; anything else there is IPv4-compatible.
        return IsUnder(b, new byte[12], 96) && !address.Equals(IPAddress.IPv6Any) && !address.Equals(IPAddress.IPv6Loopback)
            ? new IPAddress(b.AsSpan(12, 4))
            : null;
    }

    /// <summary>True when the address starts with the first <paramref name="bits"/> bits of <paramref name="prefix"/>, a whole number of bytes.</summary>
    private static bool IsUnder(byte[] address, byte[] prefix, int bits) => address.AsSpan(0, bits / 8).SequenceEqual(prefix.AsSpan(0, bits / 8));

    /// <summary>The refusal from <see cref="ConnectAsync"/>, when it is what made a request fail.</summary>
    public static RecipeSourceException? Refusal(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is RecipeSourceException refusal) return refusal;
        }

        return null;
    }

    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = IPAddress.TryParse(host, out var literal) ? [literal] : await Dns.GetHostAddressesAsync(host, cancellationToken);
        var usable = addresses.Where(IsPublic).ToArray();
        if (usable.Length == 0)
        {
            throw new RecipeSourceException(
                $"{host} is on a private network, and ChefBot only downloads from the public internet. Use a link to a public website.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(usable, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
