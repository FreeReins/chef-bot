namespace ChefBot.Core.Contracts.Messages;

/// <summary>The sign-in rules, as saved.</summary>
public sealed record SecurityResponse(int MaxFailedSignIns, int LockoutMinutes, bool RequireHttps);
