using Microsoft.AspNetCore.Identity;

namespace ChefBot.Core.Utilities;

/// <summary>Hashes passwords with ASP.NET Core's salted PBKDF2 hasher.</summary>
internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    // The hasher never reads the user it is given, so none is passed.
    private readonly PasswordHasher<object> _hasher = new();

    // A hash of a random password, made with the same settings as real ones, for VerifyNothing to check against.
    private readonly Lazy<string> _decoy;

    public IdentityPasswordHasher() =>
        _decoy = new(() => _hasher.HashPassword(null!, Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16))));

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;

    public void VerifyNothing(string password) => _hasher.VerifyHashedPassword(null!, _decoy.Value, password);
}
