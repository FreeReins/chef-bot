namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Something to put on the shopping list by hand, with or without a plan.</summary>
/// <param name="Name">What to buy, such as "milk".</param>
/// <param name="Quantity">How much, when it matters.</param>
/// <param name="Unit">The unit <paramref name="Quantity"/> is measured in.</param>
public sealed record NewShoppingItem(string Name, decimal? Quantity = null, string? Unit = null);
