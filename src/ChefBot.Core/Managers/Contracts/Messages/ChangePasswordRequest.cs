namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Change your own password while signed in.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
