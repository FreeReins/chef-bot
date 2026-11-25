namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// An amount on a shopping line that couldn't be added to its main amount, because it measures something else: the
/// 100 g in "2 tbsp + 100 g".
/// </summary>
/// <param name="Quantity">How much.</param>
/// <param name="Unit">The unit <paramref name="Quantity"/> is measured in.</param>
public sealed record AmountResponse(decimal Quantity, string Unit);
