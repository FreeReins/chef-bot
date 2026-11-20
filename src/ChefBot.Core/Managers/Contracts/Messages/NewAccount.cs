namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The details someone enters to create an account.</summary>
public sealed record NewAccount(string Username, string DisplayName, string Password);
