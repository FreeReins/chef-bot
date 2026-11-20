using System.Security.Claims;
using System.Text;
using ChefBot.Api.Contracts;
using ChefBot.Api.Https;
using ChefBot.Core.Accounts;
using ChefBot.Core.Common;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>System-wide settings: sign-in rules and HTTPS.</summary>
public static class SystemEndpoints
{
    [WolverineGet("/api/system/security")]
    public static async Task<SecurityResponse> GetSecurity(
        ClaimsPrincipal principal, [FromServices] AccountService accounts, CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return (await accounts.GetSecuritySettingsAsync(cancellationToken)).ToResponse();
    }

    /// <summary>Changes the lockout rules and whether plain HTTP visitors are sent to HTTPS. Takes effect at once.</summary>
    [WolverinePut("/api/system/security")]
    public static async Task<SecurityResponse> UpdateSecurity(
        SecurityRequest request,
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] CertificateStore certificates,
        [FromServices] HttpsPolicy policy,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        if (request.RequireHttps && !certificates.IsEnabled)
        {
            throw new RuleViolationException("HTTPS is switched off (Https:Mode is Off), so it can't be required.");
        }

        var rules = await accounts.UpdateSecuritySettingsAsync(
            request.MaxFailedSignIns, request.LockoutMinutes, request.RequireHttps, cancellationToken);
        policy.RequireHttps = rules.RequireHttps;
        return rules.ToResponse();
    }

    /// <summary>How HTTPS is set up, for the setup wizard and the Settings tab.</summary>
    [WolverineGet("/api/system/https")]
    public static HttpsStatusResponse GetHttps(
        HttpContext http, [FromServices] CertificateStore certificates, [FromServices] HttpsPolicy policy) =>
        Describe(http, certificates, policy);

    /// <summary>Creates a new self-signed certificate for the given names and starts using it straight away.</summary>
    [WolverinePost("/api/system/https/certificate")]
    public static async Task<HttpsStatusResponse> RegenerateCertificate(
        NewCertificateRequest request,
        HttpContext http,
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] CertificateStore certificates,
        [FromServices] HttpsPolicy policy,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
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
