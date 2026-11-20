using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ChefBot.Core.Common;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace ChefBot.Api.Https;

/// <summary>
/// Holds the HTTPS certificate. In self-signed mode it creates the certificate on first start,
/// keeps it on disk so browsers that trusted it keep trusting it, renews it before it expires,
/// and can replace it while ChefBot is running when an admin changes the names it covers.
/// </summary>
public sealed class CertificateStore(HttpsOptions options, string folder)
{
    private const string ServerAuthentication = "1.3.6.1.5.5.7.3.1";

    /// <summary>Kept under the 825-day limit some browsers place on certificates people trust by hand.</summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(820);

    private static readonly TimeSpan RenewBefore = TimeSpan.FromDays(30);

    private readonly Lock _gate = new();
    private volatile X509Certificate2? _current;
    private volatile IReadOnlyList<string> _hostnames = [];

    public HttpsOptions Options => options;

    public bool IsEnabled => options.Mode != HttpsMode.Off;

    /// <summary>True when ChefBot owns the certificate and can create a new one.</summary>
    public bool CanRegenerate => options.Mode == HttpsMode.SelfSigned;

    public X509Certificate2? Current => _current;

    /// <summary>The names and addresses the self-signed certificate covers.</summary>
    public IReadOnlyList<string> Hostnames => _hostnames;

    private string CertificateFile => Path.Combine(folder, "chefbot-https.pfx");

    private string HostnamesFile => Path.Combine(folder, "chefbot-https.hosts");

    /// <summary>Tells Kestrel which ports to listen on and where to get the certificate.</summary>
    public void Configure(KestrelServerOptions kestrel)
    {
        kestrel.ListenAnyIP(options.HttpPort);
        if (IsEnabled)
        {
            // The selector is asked on every connection, so a regenerated certificate is used straight away.
            kestrel.ListenAnyIP(options.Port, listen => listen.UseHttps(https => https.ServerCertificateSelector = (_, _) => _current));
        }
    }

    /// <summary>Loads the certificate at startup, creating or renewing a self-signed one when needed.</summary>
    public void Load()
    {
        switch (options.Mode)
        {
            case HttpsMode.Certificate:
                _current = LoadProvided();
                break;

            case HttpsMode.SelfSigned:
                var saved = TryLoadSaved();
                if (saved is not null && saved.NotAfter.ToUniversalTime() - RenewBefore > DateTime.UtcNow)
                {
                    _current = saved;
                    _hostnames = File.Exists(HostnamesFile) ? File.ReadAllLines(HostnamesFile) : [];
                }
                else
                {
                    var names = File.Exists(HostnamesFile) ? File.ReadAllLines(HostnamesFile) : options.Hostnames.Split(',');
                    Regenerate(names);
                }

                break;
        }
    }

    /// <summary>Creates a new self-signed certificate for the given names and starts using it.</summary>
    /// <exception cref="RuleViolationException">ChefBot doesn't own the certificate, or a name isn't valid.</exception>
    public void Regenerate(IEnumerable<string> hostnames)
    {
        if (!CanRegenerate)
        {
            throw new RuleViolationException("ChefBot only creates certificates when Https:Mode is SelfSigned.");
        }

        var names = Clean(hostnames);
        lock (_gate)
        {
            var bytes = CreateSelfSigned(names);
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(CertificateFile, bytes);
            File.WriteAllLines(HostnamesFile, names);
            _current = X509CertificateLoader.LoadPkcs12(bytes, null);
            _hostnames = names;
        }
    }

    /// <summary>The public certificate as PEM text, for people who want their device to trust it.</summary>
    public string? ExportPem() => _current?.ExportCertificatePem();

    private static List<string> Clean(IEnumerable<string> hostnames)
    {
        var names = hostnames
            .Select(name => name.Trim().ToLowerInvariant())
            .Where(name => name.Length > 0)
            .Append("localhost")
            .Distinct()
            .ToList();

        var invalid = names.FirstOrDefault(name => Uri.CheckHostName(name) == UriHostNameType.Unknown);
        return invalid is null
            ? names
            : throw new RuleViolationException($"\"{invalid}\" isn't a valid host name or IP address.");
    }

    private static byte[] CreateSelfSigned(IReadOnlyList<string> names)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={names[0]}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var alternativeNames = new SubjectAlternativeNameBuilder();
        alternativeNames.AddIpAddress(IPAddress.Loopback);
        foreach (var name in names)
        {
            if (IPAddress.TryParse(name, out var address))
            {
                alternativeNames.AddIpAddress(address);
            }
            else
            {
                alternativeNames.AddDnsName(name);
            }
        }

        request.CertificateExtensions.Add(alternativeNames.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid(ServerAuthentication) }, false));

        var now = DateTimeOffset.UtcNow;
        using var certificate = request.CreateSelfSigned(now.AddDays(-1), now + Lifetime);
        return certificate.Export(X509ContentType.Pkcs12);
    }

    private X509Certificate2? TryLoadSaved()
    {
        try
        {
            return File.Exists(CertificateFile) ? X509CertificateLoader.LoadPkcs12FromFile(CertificateFile, null) : null;
        }
        catch (CryptographicException)
        {
            return null; // An unreadable file is replaced with a fresh certificate.
        }
    }

    private X509Certificate2 LoadProvided()
    {
        if (!File.Exists(options.CertificatePath))
        {
            throw new InvalidOperationException(
                $"Https:Mode is Certificate, but the file in Https:CertificatePath wasn't found: '{options.CertificatePath}'.");
        }

        var isPfx = Path.GetExtension(options.CertificatePath).ToLowerInvariant() is ".pfx" or ".p12";
        if (isPfx)
        {
            var password = options.CertificatePassword.Length > 0 ? options.CertificatePassword : null;
            return X509CertificateLoader.LoadPkcs12FromFile(options.CertificatePath, password);
        }

        // Re-imported from PKCS#12 so the private key is usable by the server on every platform.
        using var pem = X509Certificate2.CreateFromPemFile(
            options.CertificatePath, options.KeyPath.Length > 0 ? options.KeyPath : null);
        return X509CertificateLoader.LoadPkcs12(pem.Export(X509ContentType.Pkcs12), null);
    }
}
