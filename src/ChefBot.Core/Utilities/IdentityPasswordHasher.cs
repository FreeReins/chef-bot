using Microsoft.AspNetCore.Identity;

namespace ChefBot.Core.Utilities;

/// <summary>Hashes passwords with ASP.NET Core's salted PBKDF2 hasher.</summary>
internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    // The hasher never reads the user it is given, so none is passed.
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
