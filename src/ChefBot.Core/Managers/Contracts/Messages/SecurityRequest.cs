namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Sign-in rules.</summary>
/// <param name="MaxFailedSignIns">Wrong passwords in a row before an account is locked. Zero turns lockout off.</param>
/// <param name="LockoutMinutes">How long a locked account stays locked.</param>
/// <param name="RequireHttps">Send plain HTTP visitors to HTTPS.</param>
public sealed record SecurityRequest(int MaxFailedSignIns, int LockoutMinutes, bool RequireHttps);
