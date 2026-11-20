using System.Text;
using ChefBot.Api.Https;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>System-wide settings: sign-in rules and HTTPS.</summary>
public static class SystemEndpoints
{
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverineGet("/api/system/security")]
    public static Task<SecurityResponse> GetSecurity([FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.GetSecurityAsync(cancellationToken);

    /// <summary>Changes the lockout rules and whether plain HTTP visitors are sent to HTTPS. Takes effect at once.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverinePut("/api/system/security")]
    public static Task<SecurityResponse> UpdateSecurity(
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

        return accounts.UpdateSecurityAsync(request, cancellationToken);
    }

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
