using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>A freshly worked-out shopping list, with what was left off it and why.</summary>
/// <param name="Items">What still has to be bought.</param>
/// <param name="CoveredByPantry">Needed ingredients that are already in the pantry.</param>
/// <param name="AssumedStaples">Needed ingredients treated as always on hand.</param>
internal sealed record ShoppingDraft(
    IReadOnlyList<ShoppingItem> Items,
    IReadOnlyList<string> CoveredByPantry,
    IReadOnlyList<string> AssumedStaples)
{
    /// <summary>Plain sentences explaining what was left off the list.</summary>
    public IEnumerable<string> Notes()
    {
        if (CoveredByPantry.Count > 0)
        {
            yield return $"Already in your pantry: {string.Join(", ", CoveredByPantry)}.";
        }

        if (AssumedStaples.Count > 0)
        {
            yield return $"Assumed to be on hand: {string.Join(", ", AssumedStaples)}.";
        }
    }
}
