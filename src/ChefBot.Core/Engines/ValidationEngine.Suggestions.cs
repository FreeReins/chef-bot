using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <content>Checks on AI suggestions for recipes.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>The recipe's suggestion, when there is one.</summary>
    /// <exception cref="NotFoundException">The recipe has no suggestions.</exception>
    public static RecipeSuggestion Suggestion(RecipeSuggestion? suggestion) =>
        suggestion ?? throw new NotFoundException("This recipe has no suggestions. Ask for some with Improve with AI.");

    /// <summary>The changes to make: suggestions still waiting, for the recipe as it is now, with at least one chosen.</summary>
    /// <param name="suggestion">The recipe's suggestion.</param>
    /// <param name="fingerprint">The recipe's fingerprint now.</param>
    /// <param name="changeIds">The changes chosen.</param>
    /// <exception cref="NotFoundException">The suggestions were already applied or dismissed.</exception>
    /// <exception cref="ConflictException">The recipe changed after the suggestions were made.</exception>
    /// <exception cref="RuleViolationException">None of the chosen changes is one of the suggestions.</exception>
    public static IReadOnlyList<SuggestedChange> ChangesToApply(RecipeSuggestion suggestion, string fingerprint, IReadOnlyList<int>? changeIds)
    {
        if (suggestion.Status != RecipeSuggestionStatus.Waiting)
        {
            throw new NotFoundException("These suggestions were already applied or dismissed. Ask for new ones with Improve with AI.");
        }

        if (suggestion.BasedOn != fingerprint)
        {
            throw new ConflictException("This recipe changed after the suggestions were made. Ask for new ones with Improve with AI.");
        }

        var wanted = (changeIds ?? []).ToHashSet();
        var chosen = suggestion.Changes.Where(change => wanted.Contains(change.Id)).ToList();
        return chosen.Count > 0
            ? chosen
            : throw new RuleViolationException("Tick at least one suggestion to apply, or dismiss them.");
    }
}
