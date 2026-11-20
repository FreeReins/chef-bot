namespace ChefBot.Core.Utilities;

/// <summary>Hashes passwords so they can be checked without being stored.</summary>
internal interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}
