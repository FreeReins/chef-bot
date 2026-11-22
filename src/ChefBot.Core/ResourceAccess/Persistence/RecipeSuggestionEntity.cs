using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class RecipeSuggestionEntity
{
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }

    public string BasedOn { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public RecipeSuggestionStatus Status { get; set; }

    /// <summary>The suggested changes as JSON.</summary>
    public string Changes { get; set; } = "[]";
}
