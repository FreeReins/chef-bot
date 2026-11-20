using System.Text.Json.Serialization;

namespace ChefBot.Core.Contracts.Messages;

// Requests, responses and events of IAccountManager: setup, signing in, people, links and sign-in rules.

/// <summary>What a single-use link lets its holder do.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LinkPurpose>))]
public enum LinkPurpose
{
    /// <summary>Create a new account.</summary>
    Invite,

    /// <summary>Choose a new password for an existing account.</summary>
    PasswordReset,
}

/// <summary>The details someone enters to create an account.</summary>
public sealed record NewAccount(string Username, string DisplayName, string Password);

public sealed record SignInRequest(string Username, string Password);

/// <summary>Create an account from an invite link.</summary>
public sealed record AcceptInviteRequest(string Token, string Username, string DisplayName, string Password);

/// <summary>Choose a new password using a reset link.</summary>
public sealed record ResetPasswordRequest(string Token, string Password);

/// <summary>Change your own password while signed in.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Sign-in rules.</summary>
/// <param name="MaxFailedSignIns">Wrong passwords in a row before an account is locked. Zero turns lockout off.</param>
/// <param name="LockoutMinutes">How long a locked account stays locked.</param>
/// <param name="RequireHttps">Send plain HTTP visitors to HTTPS.</param>
public sealed record SecurityRequest(int MaxFailedSignIns, int LockoutMinutes, bool RequireHttps);

/// <summary>An account, as shown to people.</summary>
/// <param name="Id">The account's id.</param>
/// <param name="Username">The name used to sign in.</param>
/// <param name="DisplayName">The name shown in the app.</param>
/// <param name="IsAdmin">True when the account can manage people and settings.</param>
/// <param name="CreatedAt">When the account was created.</param>
/// <param name="LockedUntil">Set while the account is locked after too many wrong passwords.</param>
public sealed record UserResponse(
    Guid Id, string Username, string DisplayName, bool IsAdmin, DateTimeOffset CreatedAt, DateTimeOffset? LockedUntil);

/// <summary>What a sign-in cookie needs to know about an account, and checks again on every request.</summary>
/// <param name="UserId">The account's id.</param>
/// <param name="Username">The name used to sign in.</param>
/// <param name="IsAdmin">True when the account can manage people and settings.</param>
/// <param name="SessionVersion">Goes up when the password changes, which ends older sessions.</param>
public sealed record SessionInfo(Guid UserId, string Username, bool IsAdmin, int SessionVersion);

/// <summary>An account that has just signed in, or been created or changed in a way that starts a new session.</summary>
public sealed record AccountSession(UserResponse User, SessionInfo Session);

/// <summary>What the page needs to decide which screen to show.</summary>
/// <param name="SetupRequired">True until the first account exists.</param>
/// <param name="User">The signed-in account, or null.</param>
/// <param name="MealieConfigured">True when importing from Mealie is possible.</param>
public sealed record SessionResponse(bool SetupRequired, UserResponse? User, bool MealieConfigured);

public sealed record SecurityResponse(int MaxFailedSignIns, int LockoutMinutes, bool RequireHttps);

/// <summary>A freshly created link. The token is only available now: it is stored as a hash.</summary>
public sealed record IssuedLink(Guid Id, LinkPurpose Purpose, string Token, DateTimeOffset ExpiresAt);

/// <summary>A link that hasn't been used yet, as shown to admins. The token can't be shown again.</summary>
/// <param name="Id">The link's id.</param>
/// <param name="Purpose">Whether it is an invite or a password reset.</param>
/// <param name="Username">Whose password it resets. Null for invites.</param>
/// <param name="CreatedAt">When the link was created.</param>
/// <param name="ExpiresAt">When the link stops working.</param>
public sealed record PendingLink(Guid Id, LinkPurpose Purpose, string? Username, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

/// <summary>What a link is for, shown to the person who opened it.</summary>
public sealed record LinkDetails(LinkPurpose Purpose, string? Username, DateTimeOffset ExpiresAt);

/// <summary>Event: the sign-in rules changed, including whether HTTPS is required.</summary>
public sealed record SecuritySettingsChanged(bool RequireHttps);
