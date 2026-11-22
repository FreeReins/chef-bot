using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>What an AI model suggested a recipe is missing. A recipe has at most one; a newer one replaces it.</summary>
internal sealed class RecipeSuggestion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RecipeId { get; set; }

    /// <summary>A fingerprint of the recipe's text when the suggestions were made; they are stale once it differs.</summary>
    public string BasedOn { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public RecipeSuggestionStatus Status { get; set; }

    public List<SuggestedChange> Changes { get; set; } = [];
}
