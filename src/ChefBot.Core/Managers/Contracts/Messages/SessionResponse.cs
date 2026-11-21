namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What the page needs to decide which screen to show.</summary>
/// <param name="SetupRequired">True until the first account exists.</param>
/// <param name="User">The signed-in account, or null.</param>
public sealed record SessionResponse(bool SetupRequired, UserResponse? User);
