using System.Net;
using ChefBot.Api;

namespace ChefBot.Core.Tests;

/// <summary>One network address can only try passwords and secret links so often.</summary>
public class SignInLimitTests
{
    [Test]
    [Arguments("/api/account/sign-in")]
    [Arguments("/API/Account/Sign-In/")]
    [Arguments("/api/setup/account")]
    [Arguments("/api/account/accept-invite")]
    [Arguments("/api/account/reset-password")]
    [Arguments("/api/account/link")]
    public async Task Forms_that_take_a_password_or_link_are_limited(string path) =>
        await Assert.That(SignInLimits.IsLimited(path)).IsTrue();

    [Test]
    [Arguments("/api/account/password")]
    [Arguments("/api/recipes")]
    [Arguments("/api/session")]
    public async Task Everything_else_is_not(string path) =>
        await Assert.That(SignInLimits.IsLimited(path)).IsFalse();

    [Test]
    public async Task An_ipv6_home_counts_as_one_address()
    {
        var one = SignInLimits.Partition(IPAddress.Parse("2001:db8:1:2:aaaa::1"));
        var other = SignInLimits.Partition(IPAddress.Parse("2001:db8:1:2:bbbb::9"));
        var neighbour = SignInLimits.Partition(IPAddress.Parse("2001:db8:1:3::1"));

        await Assert.That(one).IsEqualTo(other);
        await Assert.That(one).IsNotEqualTo(neighbour);
    }

    [Test]
    public async Task Ipv4_addresses_count_on_their_own_however_they_arrive()
    {
        await Assert.That(SignInLimits.Partition(IPAddress.Parse("192.168.1.20"))).IsEqualTo("192.168.1.20");
        await Assert.That(SignInLimits.Partition(IPAddress.Parse("::ffff:192.168.1.20"))).IsEqualTo("192.168.1.20");
        await Assert.That(SignInLimits.Partition(IPAddress.Parse("192.168.1.21"))).IsNotEqualTo("192.168.1.20");
    }
}
