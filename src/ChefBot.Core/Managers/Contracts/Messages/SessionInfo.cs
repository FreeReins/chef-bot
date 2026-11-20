namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What a sign-in cookie needs to know about an account, and checks again on every request.</summary>
/// <param name="UserId">The account's id.</param>
/// <param name="Username">The name used to sign in.</param>
/// <param name="IsAdmin">True when the account can manage people and settings.</param>
/// <param name="SessionVersion">Goes up when the password changes, which ends older sessions.</param>
public sealed record SessionInfo(Guid UserId, string Username, bool IsAdmin, int SessionVersion);
