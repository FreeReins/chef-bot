namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Every to-do app the shopping list can be sent to, in the order they are offered. Ones left out are removed.</summary>
/// <param name="Targets">The targets, at most ten.</param>
public sealed record SaveShoppingTargetsRequest(IReadOnlyList<ShoppingTargetDraft> Targets);
