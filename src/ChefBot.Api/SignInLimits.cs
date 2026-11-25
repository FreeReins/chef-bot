using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace ChefBot.Api;

/// <summary>
/// Limits how often one network address can use the forms that take a password or a secret link: signing in, first-run
/// setup, accepting an invite, resetting a password and looking up a link. That slows down anyone guessing passwords
/// across many accounts, which account lockout alone doesn't. Behind a reverse proxy, list it in Https:TrustedProxies
/// so each visitor's own address is used rather than the proxy's.
/// </summary>
public static class SignInLimits
{
    /// <summary>Attempts one address may make in <see cref="Window"/>.</summary>
    public const int PerWindow = 10;

    /// <summary>The window attempts are counted over.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>What a request over the limit is told.</summary>
    public const string Refusal = "Too many tries from this network in a short time. Wait a minute, then try again.";

    private static readonly string[] Limited =
    [
        "/api/account/sign-in",
        "/api/setup/account",
        "/api/account/accept-invite",
        "/api/account/reset-password",
        "/api/account/link",
    ];

    /// <summary>True for the requests that are limited.</summary>
    public static bool IsLimited(string path) => Limited.Any(limited => string.Equals(path.TrimEnd('/'), limited, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Which group an address counts in: an IPv4 address on its own, an IPv6 address with the rest of its /64, since
    /// one home or phone is usually given a whole /64 and could otherwise use a new address for every try.
    /// </summary>
    public static string Partition(IPAddress? address)
    {
        if (address is null)
        {
            return "unknown";
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes) + "/64";
    }

    /// <summary>Registers the limiter.</summary>
    public static IServiceCollection AddChefBotSignInLimits(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                IsLimited(http.Request.Path)
                    ? RateLimitPartition.GetFixedWindowLimiter(Partition(http.Connection.RemoteIpAddress), _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = PerWindow,
                        Window = Window,
                        QueueLimit = 0,
                    })
                    : RateLimitPartition.GetNoLimiter(string.Empty));
            options.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                http.Response.Headers.RetryAfter = ((int)Window.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = http,
                    ProblemDetails = new ProblemDetails { Status = StatusCodes.Status429TooManyRequests, Title = "Too many tries", Detail = Refusal },
                });
            };
        });
}
