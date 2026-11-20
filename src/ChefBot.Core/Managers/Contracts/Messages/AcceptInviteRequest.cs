namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Create an account from an invite link.</summary>
public sealed record AcceptInviteRequest(string Token, string Username, string DisplayName, string Password);
