namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>The result of counting a sign-in attempt before its password is checked.</summary>
/// <param name="Number">
/// How many attempts have been counted since the last successful sign-in or the end of a lockout, this one included.
/// Null when the attempt wasn't counted because the account is locked, or no longer exists.
/// </param>
/// <param name="LockedUntil">When the account is locked, when it unlocks.</param>
internal sealed record SignInAttempt(int? Number, DateTimeOffset? LockedUntil);
