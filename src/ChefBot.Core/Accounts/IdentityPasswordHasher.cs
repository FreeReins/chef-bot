using ChefBot.Core.Abstractions;
using ChefBot.Core.Accounts;
using Microsoft.AspNetCore.Identity;

namespace ChefBot.Core.Accounts;

/// <summary>Hashes passwords with ASP.NET Core's salted PBKDF2 hasher.</summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    // The hasher never reads the user it is given, so none is passed.
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
