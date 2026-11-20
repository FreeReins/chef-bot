using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>Stored recipes.</summary>
internal interface IRecipeAccess
{
    /// <summary>Recipes that can be planned, with their ingredients, by name.</summary>
    Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>Every recipe, archived ones included.</summary>
    Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken);

    Task<Recipe?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Adds the recipe, or updates the stored one with the same id.</summary>
    Task SaveAsync(Recipe recipe, CancellationToken cancellationToken);
}
