namespace ChefBot.Core.Contracts.Messages;

/// <summary>Sign in with a username and password.</summary>
public sealed record SignInRequest(string Username, string Password);
