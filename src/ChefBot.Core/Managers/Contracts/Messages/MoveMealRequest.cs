namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Moves a dinner or breakfast to another day of its week. A meal of the same kind already there swaps places with it.</summary>
/// <param name="Date">The day to move the meal to.</param>
public sealed record MoveMealRequest(DateOnly Date);
