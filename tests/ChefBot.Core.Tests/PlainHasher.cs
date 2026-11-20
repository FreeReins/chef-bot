using ChefBot.Core.Utilities;

namespace ChefBot.Core.Tests;

internal sealed class PlainHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password.Length}:{password.GetHashCode()}";

    public bool Verify(string hash, string password) => hash == Hash(password);
}
