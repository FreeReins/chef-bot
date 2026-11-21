using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>Stored recipes.</summary>
internal interface IRecipeAccess
{
    /// <summary>Recipes that can be planned, with their ingredients, by name.</summary>
    Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>Every recipe, archived ones included.</summary>
    Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>A slice of the recipes that can be planned matching the query, with their ingredients and photos.</summary>
    Task<RecipeSearchResult> SearchActiveAsync(RecipeQuery query, CancellationToken cancellationToken);

    /// <summary>How many recipes can be planned.</summary>
    Task<int> CountActiveAsync(CancellationToken cancellationToken);

    Task<Recipe?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Adds the recipe, or updates the stored one with the same id.</summary>
    Task SaveAsync(Recipe recipe, CancellationToken cancellationToken);

    /// <summary>A photo's bytes, or null when the recipe has no stored photo with that id.</summary>
    Task<RecipeImageFile?> GetImageFileAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a photo's bytes, replacing any already stored for it. The photo itself must be on its recipe, saved
    /// with <see cref="SaveAsync"/> in the same commit, with its content type set.
    /// </summary>
    Task SaveImageFileAsync(RecipeImageFile file, CancellationToken cancellationToken);
}
