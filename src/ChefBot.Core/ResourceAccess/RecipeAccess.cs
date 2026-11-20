using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IRecipeAccess"/>
internal sealed class RecipeAccess(ChefBotDbContext db) : IRecipeAccess
{
    private IQueryable<RecipeEntity> WithIngredients => db.Recipes.Include(recipe => recipe.Ingredients);

    public async Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken) =>
        (await WithIngredients.Where(recipe => !recipe.IsArchived).OrderBy(recipe => recipe.Name).ToListAsync(cancellationToken))
            .Select(recipe => recipe.ToContract()).ToList();

    public async Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken) =>
        (await WithIngredients.ToListAsync(cancellationToken)).Select(recipe => recipe.ToContract()).ToList();

    public async Task<Recipe?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(id, cancellationToken))?.ToContract();

    public async Task SaveAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        var fresh = recipe.ToEntity();
        var tracked = await TrackedAsync(recipe.Id, cancellationToken);
        if (tracked is null)
        {
            db.Recipes.Add(fresh);
            return;
        }

        db.CopyInto(tracked, fresh);
        db.SyncChildren(tracked.Ingredients, fresh.Ingredients, ingredient => ingredient.Id);
    }

    private async Task<RecipeEntity?> TrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.Recipes.Local.FirstOrDefault(recipe => recipe.Id == id)
        ?? await WithIngredients.FirstOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);
}
