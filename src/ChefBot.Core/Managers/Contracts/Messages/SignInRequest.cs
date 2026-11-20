namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Sign in with a username and password.</summary>
public sealed record SignInRequest(string Username, string Password);
