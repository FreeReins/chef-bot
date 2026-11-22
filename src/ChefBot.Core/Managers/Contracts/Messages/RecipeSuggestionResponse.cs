namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What an AI model suggests a recipe is missing.</summary>
/// <param name="RecipeId">The recipe.</param>
/// <param name="ModelName">The name of the model that made the suggestions.</param>
/// <param name="CreatedAt">When they were made.</param>
/// <param name="Status">Where they stand.</param>
/// <param name="Stale">True when the recipe was changed after they were made, so they can't be applied; ask for new ones.</param>
/// <param name="Changes">The changes, to tick or leave. Empty when the model had nothing to add.</param>
public sealed record RecipeSuggestionResponse(
    Guid RecipeId,
    string ModelName,
    DateTimeOffset CreatedAt,
    RecipeSuggestionStatus Status,
    bool Stale,
    IReadOnlyList<SuggestedChange> Changes);
