namespace ChefBot.Core.Utilities;

/// <summary>Hashes passwords so they can be checked without being stored.</summary>
internal interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);

    /// <summary>
    /// Takes as long as <see cref="Verify"/> and checks nothing. A refused sign-in calls it when no password was
    /// checked, so how long the answer takes doesn't show whether the username exists.
    /// </summary>
    void VerifyNothing(string password);
}
