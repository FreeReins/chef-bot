namespace ChefBot.Core.Contracts.Messages;

/// <summary>Choose a new password using a reset link.</summary>
public sealed record ResetPasswordRequest(string Token, string Password);
