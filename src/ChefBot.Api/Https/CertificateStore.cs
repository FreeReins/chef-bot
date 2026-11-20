using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ChefBot.Core.Common;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace ChefBot.Api.Https;

/// <summary>
/// Holds the HTTPS certificate. In self-signed mode it creates the certificate on first start, keeps it on disk
/// so browsers that trusted it keep trusting it, renews it before it expires, and can replace it while ChefBot is
/// running when an admin changes the names it covers. In certificate mode it serves the certificate you provide,
/// with its intermediate certificates, and picks up a renewed one (for example from Let's Encrypt) by itself.
/// </summary>
public sealed class CertificateStore(HttpsOptions options, string folder) : IDisposable
{
    private const string ServerAuthentication = "1.3.6.1.5.5.7.3.1";

    /// <summary>Kept under the 825-day limit some browsers place on certificates people trust by hand.</summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(820);

    private static readonly TimeSpan RenewBefore = TimeSpan.FromDays(30);

    /// <summary>How often a provided certificate is checked for a renewal, and a self-signed one for its expiry.</summary>
    private static readonly TimeSpan CheckEvery = TimeSpan.FromMinutes(1);

    private readonly Lock _gate = new();
    private volatile Served? _served;
    private volatile IReadOnlyList<string> _hostnames = [];
    private byte[]? _providedFiles;
    private Timer? _timer;

    public HttpsOptions Options => options;

    public bool IsEnabled => options.Mode != HttpsMode.Off;

    /// <summary>True when ChefBot owns the certificate and can create a new one.</summary>
    public bool CanRegenerate => options.Mode == HttpsMode.SelfSigned;

    public X509Certificate2? Current => _served?.Certificate;

    /// <summary>The names and addresses the self-signed certificate covers.</summary>
    public IReadOnlyList<string> Hostnames => _hostnames;

    /// <summary>Set once logging is available, after startup. Renewals and problems found later are logged here.</summary>
    public ILogger? Logger { get; set; }

    private string CertificateFile => Path.Combine(folder, "chefbot-https.pfx");

    private string HostnamesFile => Path.Combine(folder, "chefbot-https.hosts");

    /// <summary>Tells Kestrel which ports to listen on and where to get the certificate.</summary>
    public void Configure(KestrelServerOptions kestrel)
    {
        kestrel.ListenAnyIP(options.HttpPort);
        if (IsEnabled)
        {
            // Asked on every connection, so a regenerated or renewed certificate is used straight away. The context
            // carries the intermediate certificates, which some clients (Android, curl) need to trust the server.
            kestrel.ListenAnyIP(options.Port, listen => listen.UseHttps(new TlsHandshakeCallbackOptions
            {
                OnConnection = _ => ValueTask.FromResult(new SslServerAuthenticationOptions
                {
                    ServerCertificateContext = _served?.Context,
                }),
            }));
        }
    }

    /// <summary>Loads the certificate at startup, creating or renewing a self-signed one when needed.</summary>
    /// <exception cref="InvalidOperationException">The certificate can't be read, or the keys folder can't be written.</exception>
    public void Load()
    {
        switch (options.Mode)
        {
            case HttpsMode.Certificate:
                _providedFiles = ReadProvidedFiles();
                _served = LoadProvided();
                break;

            case HttpsMode.SelfSigned:
                var saved = TryLoadSaved();
                if (saved is not null && !IsDueForRenewal(saved))
                {
                    _served = Serve(saved, []);
                    _hostnames = File.Exists(HostnamesFile) ? File.ReadAllLines(HostnamesFile) : [];
                }
                else
                {
                    var names = File.Exists(HostnamesFile) ? File.ReadAllLines(HostnamesFile) : options.Hostnames.Split(',');
                    Regenerate(names);
                }

                break;

            default:
                return;
        }

        _timer = new Timer(_ => CheckForChanges(), null, CheckEvery, CheckEvery);
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
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(CertificateFile, bytes);
                File.WriteAllLines(HostnamesFile, names);
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
            {
                throw DataFolders.CannotWrite(folder, "Data:SessionKeys", exception);
            }

            _served = Serve(X509CertificateLoader.LoadPkcs12(bytes, null), []);
            _hostnames = names;
        }
    }

    /// <summary>The public certificate as PEM text, for people who want their device to trust it.</summary>
    public string? ExportPem() => _served?.Certificate.ExportCertificatePem();

    public void Dispose() => _timer?.Dispose();

    /// <summary>
    /// Runs every minute. A provided certificate whose files changed is loaded again; a broken renewal leaves the
    /// current certificate in place and is retried. A self-signed certificate is renewed before it expires.
    /// </summary>
    private void CheckForChanges()
    {
        try
        {
            if (options.Mode == HttpsMode.Certificate)
            {
                var files = ReadProvidedFiles();
                if (_providedFiles is not null && files.AsSpan().SequenceEqual(_providedFiles))
                {
                    return;
                }

                _served = LoadProvided();
                _providedFiles = files;
                Logger?.LogInformation("Loaded the changed certificate from {Path}. It is valid until {Expires:d}.",
                    options.CertificatePath, _served.Certificate.NotAfter);
            }
            else if (options.Mode == HttpsMode.SelfSigned && _served is { } served && IsDueForRenewal(served.Certificate))
            {
                Regenerate(_hostnames);
                Logger?.LogInformation("Renewed the self-signed certificate. Devices that trusted the old one need the new one.");
            }
        }
        catch (Exception exception)
        {
            Logger?.LogWarning("Kept the current HTTPS certificate: {Reason}", exception.Message);
        }
    }

    private static bool IsDueForRenewal(X509Certificate2 certificate) =>
        certificate.NotAfter.ToUniversalTime() - RenewBefore <= DateTime.UtcNow;

    private static Served Serve(X509Certificate2 certificate, X509Certificate2Collection intermediates) =>
        new(certificate, SslStreamCertificateContext.Create(certificate, intermediates, offline: true));

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

    /// <summary>The bytes of the certificate and key files, to notice when either is replaced.</summary>
    private byte[] ReadProvidedFiles()
    {
        RequireProvidedFile(options.CertificatePath, "Https:CertificatePath");
        var paths = options.KeyPath.Length > 0 ? new[] { options.CertificatePath, options.KeyPath } : [options.CertificatePath];
        try
        {
            return paths.SelectMany(File.ReadAllBytes).ToArray();
        }
        catch (UnauthorizedAccessException exception)
        {
            throw Unreadable(exception);
        }
    }

    /// <summary>Loads the provided certificate, its private key, and any intermediate certificates in the same file.</summary>
    private Served LoadProvided()
    {
        var path = options.CertificatePath;
        RequireProvidedFile(path, "Https:CertificatePath");
        if (options.KeyPath.Length > 0)
        {
            RequireProvidedFile(options.KeyPath, "Https:KeyPath");
        }

        try
        {
            X509Certificate2 certificate;
            var everything = new X509Certificate2Collection();
            if (Path.GetExtension(path).ToLowerInvariant() is ".pfx" or ".p12")
            {
                var password = options.CertificatePassword.Length > 0 ? options.CertificatePassword : null;
                everything = X509CertificateLoader.LoadPkcs12CollectionFromFile(path, password);
                certificate = everything.FirstOrDefault(candidate => candidate.HasPrivateKey)
                    ?? throw new InvalidOperationException($"The file in Https:CertificatePath ('{path}') has no private key.");
            }
            else
            {
                // A PEM file may hold the whole chain (fullchain.pem); the first certificate is the server's own.
                everything.ImportFromPemFile(path);
                using var pem = X509Certificate2.CreateFromPemFile(path, options.KeyPath.Length > 0 ? options.KeyPath : null);

                // Re-imported from PKCS#12 so the private key is usable by the server on every platform.
                certificate = X509CertificateLoader.LoadPkcs12(pem.Export(X509ContentType.Pkcs12), null);
            }

            // Intermediates only: the root is already on the client, and sending it is wasted bytes.
            var intermediates = new X509Certificate2Collection(everything
                .Where(candidate => candidate.Thumbprint != certificate.Thumbprint && candidate.Subject != candidate.Issuer)
                .ToArray());
            return Serve(certificate, intermediates);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw Unreadable(exception);
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException(
                $"The certificate in Https:CertificatePath ('{path}') couldn't be used: {exception.Message} " +
                "Use a PEM certificate (fullchain.pem works) with its key in Https:KeyPath, or a .pfx file with " +
                "Https:CertificatePassword if it has one.", exception);
        }
    }

    private static void RequireProvidedFile(string path, string setting)
    {
        if (path.Length == 0)
        {
            throw new InvalidOperationException($"Https:Mode is Certificate, so {setting} needs to point at a file.");
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Https:Mode is Certificate, but the file in {setting} wasn't found: '{path}'. In a container, check that it is mounted.");
        }
    }

    private static InvalidOperationException Unreadable(UnauthorizedAccessException exception) => new(
        $"ChefBot can't read the certificate or key ({exception.Message}). It runs as user {DataFolders.CurrentUser} " +
        "(1654 in the container image), so that user needs read access: for example, copy the key to a file owned by " +
        "that user, or make it readable to its group.", exception);

    /// <summary>The certificate being served, and the same with its intermediates ready for the TLS handshake.</summary>
    private sealed record Served(X509Certificate2 Certificate, SslStreamCertificateContext Context);
}
