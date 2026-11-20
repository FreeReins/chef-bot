namespace ChefBot.Api.Https;

/// <summary>The names and addresses a new self-signed certificate should cover.</summary>
public sealed record NewCertificateRequest(IReadOnlyList<string> Hostnames);
