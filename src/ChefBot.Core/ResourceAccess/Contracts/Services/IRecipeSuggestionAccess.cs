using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>What AI models suggested recipes are missing, one suggestion per recipe at most.</summary>
internal interface IRecipeSuggestionAccess
{
    /// <summary>The recipe's suggestion, or null when it has none.</summary>
    Task<RecipeSuggestion?> FindAsync(Guid recipeId, CancellationToken cancellationToken);

    /// <summary>The suggestions for these recipes.</summary>
    Task<List<RecipeSuggestion>> ListForRecipesAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken);

    /// <summary>Every suggestion.</summary>
    Task<List<RecipeSuggestion>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>Keeps the suggestion, replacing any the recipe already had.</summary>
    Task SaveAsync(RecipeSuggestion suggestion, CancellationToken cancellationToken);

    /// <summary>Removes the recipe's suggestion, if it has one.</summary>
    Task RemoveAsync(Guid recipeId, CancellationToken cancellationToken);
}
