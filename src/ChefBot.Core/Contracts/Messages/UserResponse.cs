namespace ChefBot.Core.Contracts.Messages;

/// <summary>An account, as shown to people.</summary>
/// <param name="Id">The account's id.</param>
/// <param name="Username">The name used to sign in.</param>
/// <param name="DisplayName">The name shown in the app.</param>
/// <param name="IsAdmin">True when the account can manage people and settings.</param>
/// <param name="CreatedAt">When the account was created.</param>
/// <param name="LockedUntil">Set while the account is locked after too many wrong passwords.</param>
public sealed record UserResponse(
    Guid Id, string Username, string DisplayName, bool IsAdmin, DateTimeOffset CreatedAt, DateTimeOffset? LockedUntil);
