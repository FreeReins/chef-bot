namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Change your own password while signed in.</summary>
/// <param name="CurrentPassword">The password in use now.</param>
/// <param name="NewPassword">The new one, at least 8 characters.</param>
/// <param name="RevokeApiTokens">True to revoke your API tokens too, so assistants need new ones.</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, bool RevokeApiTokens = false);
