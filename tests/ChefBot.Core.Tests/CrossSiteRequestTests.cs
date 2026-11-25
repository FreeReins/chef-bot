using ChefBot.Api;

namespace ChefBot.Core.Tests;

/// <summary>Changes only come from ChefBot's own page, or from outside a browser.</summary>
public class CrossSiteRequestTests
{
    [Test]
    [Arguments("POST", "same-origin", null)]
    [Arguments("DELETE", "same-origin", "https://chefbot.home.lan")]
    [Arguments("POST", "none", null)]
    [Arguments("POST", null, "https://chefbot.home.lan")]
    [Arguments("POST", null, "https://CHEFBOT.home.lan")]
    [Arguments("POST", null, null)]
    [Arguments("GET", "cross-site", "https://evil.example")]
    public async Task Requests_from_chefbots_own_page_or_outside_a_browser_go_through(string method, string? fetchSite, string? origin) =>
        await Assert.That(CrossSiteRequests.IsRefused(method, "/api/system/backups/x/restore", fetchSite, origin, "chefbot.home.lan")).IsFalse();

    [Test]
    [Arguments("POST", "same-site", "https://printer.home.lan")]
    [Arguments("POST", "cross-site", "https://evil.example")]
    [Arguments("PUT", "same-site", null)]
    [Arguments("DELETE", "cross-site", null)]
    [Arguments("POST", null, "https://printer.home.lan")]
    [Arguments("POST", null, "https://chefbot.home.lan:9443")]
    [Arguments("POST", null, "null")]
    public async Task Changes_sent_from_another_page_are_refused(string method, string? fetchSite, string? origin) =>
        await Assert.That(CrossSiteRequests.IsRefused(method, "/api/system/backups/x/restore", fetchSite, origin, "chefbot.home.lan")).IsTrue();

    [Test]
    public async Task A_non_default_port_has_to_match_too() =>
        await Assert.That(CrossSiteRequests.IsRefused("POST", "/api/recipes", null, "https://chefbot.home.lan:8443", "chefbot.home.lan:8443")).IsFalse();

    [Test]
    public async Task The_mcp_endpoint_signs_in_with_a_token_so_it_is_left_alone() =>
        await Assert.That(CrossSiteRequests.IsRefused("POST", "/mcp", "cross-site", "https://claude.ai", "chefbot.home.lan")).IsFalse();
}
