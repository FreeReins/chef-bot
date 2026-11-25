namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>An amount on a shopping line that couldn't be added to its main one, such as the 100 g in "2 tbsp + 100 g".</summary>
/// <param name="Quantity">How much.</param>
/// <param name="Unit">The unit of <paramref name="Quantity"/>.</param>
internal sealed record ShoppingAmount(decimal Quantity, string Unit);
