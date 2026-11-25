namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// A secret an assistant, such as an MCP client, signs in with as one account. It keeps working until it expires,
/// is revoked, its owner resets their password or its account is removed.
/// </summary>
internal sealed class ApiToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The account the token signs in as.</summary>
    public Guid UserId { get; set; }

    /// <summary>What its owner called it, so they can tell their tokens apart.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>A hash of the secret, so a copy of the database can't be used to get in.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it stops working, or null when it lasts until it is revoked.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>When an assistant last signed in with it, to the nearest few minutes; null when never.</summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>True when it may only use tools that change nothing.</summary>
    public bool ReadOnly { get; set; }
}
