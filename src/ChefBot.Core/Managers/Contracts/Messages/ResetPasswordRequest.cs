namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Choose a new password using a reset link.</summary>
public sealed record ResetPasswordRequest(string Token, string Password);
