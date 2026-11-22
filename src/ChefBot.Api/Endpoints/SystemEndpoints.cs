using System.Text;
using ChefBot.Api.Https;
using ChefBot.Api.Logging;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>System-wide settings: sign-in rules, HTTPS and the advanced planning settings, and the server's recent log.</summary>
public static class SystemEndpoints
{
    /// <summary>How plans weigh repeats, expiring food and perishables, and the words that sort imported recipes, with their defaults.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverineGet("/api/system/advanced")]
    public static Task<AdvancedSettingsResponse> GetAdvanced([FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.GetAdvancedSettingsAsync(cancellationToken);

    /// <summary>Changes the advanced settings. A value left out, or null, goes back to its default.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverinePut("/api/system/advanced")]
    public static Task<AdvancedSettingsResponse> SaveAdvanced(
        AdvancedSettingsRequest request, [FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.SaveAdvancedSettingsAsync(request, cancellationToken);

    /// <summary>The sign-in rules. "Always use HTTPS" reads as off while HTTPS is switched off, since it does nothing then.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverineGet("/api/system/security")]
    public static async Task<SecurityResponse> GetSecurity(
        [FromServices] IAccountManager accounts, [FromServices] CertificateStore certificates, CancellationToken cancellationToken) =>
        InEffect(await accounts.GetSecurityAsync(cancellationToken), certificates);

    /// <summary>Changes the lockout rules and whether plain HTTP visitors are sent to HTTPS. Takes effect at once.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverinePut("/api/system/security")]
    public static async Task<SecurityResponse> UpdateSecurity(
        SecurityRequest request,
        [FromServices] IAccountManager accounts,
        [FromServices] CertificateStore certificates,
        CancellationToken cancellationToken)
    {
        // Whether HTTPS can be served is up to the host, so it is checked here.
        if (request.RequireHttps && !certificates.IsEnabled)
        {
            throw new RuleViolationException("HTTPS is switched off (Https:Mode is Off), so it can't be required.");
        }

        return InEffect(await accounts.UpdateSecurityAsync(request, cancellationToken), certificates);
    }

    /// <summary>The log entries written since startup (the last <see cref="LogBuffer.Capacity"/>), oldest first. Pass
    /// <paramref name="after"/> to get only the entries newer than that sequence number.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverineGet("/api/system/logs")]
    public static LogsResponse GetLogs([FromServices] LogBuffer logs, long? after) =>
        new(logs.Since(after ?? 0), LogBuffer.Capacity);

    /// <summary>How HTTPS is set up, for the setup wizard and the Settings tab.</summary>
    [WolverineGet("/api/system/https")]
    public static HttpsStatusResponse GetHttps(
        HttpContext http, [FromServices] CertificateStore certificates, [FromServices] HttpsPolicy policy) =>
        Describe(http, certificates, policy);

    /// <summary>Creates a new self-signed certificate for the given names and starts using it straight away.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverinePost("/api/system/https/certificate")]
    public static HttpsStatusResponse RegenerateCertificate(
        NewCertificateRequest request, HttpContext http, [FromServices] CertificateStore certificates, [FromServices] HttpsPolicy policy)
    {
        certificates.Regenerate(request.Hostnames ?? []);
        return Describe(http, certificates, policy);
    }

    /// <summary>Downloads the public certificate, to install on devices so their browsers trust ChefBot.</summary>
    [WolverineGet("/api/system/https/certificate")]
    public static IResult DownloadCertificate([FromServices] CertificateStore certificates)
    {
        var pem = certificates.ExportPem() ?? throw new NotFoundException("HTTPS is switched off, so there is no certificate.");
        return Results.File(Encoding.ASCII.GetBytes(pem), "application/x-x509-ca-cert", "chefbot.crt");
    }

    /// <summary>
    /// The rules as they act now. "Always use HTTPS" saved while HTTPS was on reads as off once HTTPS is switched
    /// off, so saving other rules from the page doesn't fail on it.
    /// </summary>
    private static SecurityResponse InEffect(SecurityResponse rules, CertificateStore certificates) =>
        rules with { RequireHttps = rules.RequireHttps && certificates.IsEnabled };

    private static HttpsStatusResponse Describe(HttpContext http, CertificateStore certificates, HttpsPolicy policy) => new(
        certificates.Options.Mode,
        certificates.IsEnabled,
        certificates.Options.PublicPort,
        certificates.Hostnames,
        certificates.Current is { } certificate ? new DateTimeOffset(certificate.NotAfter.ToUniversalTime()) : null,
        certificates.CanRegenerate,
        http.Request.IsHttps,
        policy.RequireHttps);
}
