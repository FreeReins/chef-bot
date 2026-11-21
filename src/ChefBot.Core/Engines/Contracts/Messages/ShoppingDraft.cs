using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>A freshly worked-out shopping list, with what was left off it and why.</summary>
/// <param name="Items">What still has to be bought.</param>
/// <param name="CoveredByPantry">Needed ingredients that are already in the pantry.</param>
/// <param name="AssumedStaples">Needed ingredients treated as always on hand.</param>
internal sealed record ShoppingDraft(
    IReadOnlyList<ShoppingItem> Items,
    IReadOnlyList<string> CoveredByPantry,
    IReadOnlyList<string> AssumedStaples);
