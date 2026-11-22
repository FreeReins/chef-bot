using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>Favourite recipes: marking them, and who has each one.</content>
internal sealed partial class RecipeManager
{
    public async Task<RecipeSummaryResponse> AddFavouriteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var recipe = await ActiveAsync(id, cancellationToken);
        var user = await validation.UserAsync(userId, cancellationToken);
        if (await favourites.FindAsync(recipe.Id, user.Id, cancellationToken) is null)
        {
            await favourites.AddAsync(
                new RecipeFavourite { RecipeId = recipe.Id, UserId = user.Id, CreatedAt = clock.GetUtcNow() }, cancellationToken);
            await messages.CommitAsync(cancellationToken);
        }

        return await SummaryAsync(recipe, cancellationToken);
    }

    public async Task<RecipeSummaryResponse> RemoveFavouriteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var recipe = await ActiveAsync(id, cancellationToken);
        if (await favourites.FindAsync(recipe.Id, userId, cancellationToken) is { } favourite)
        {
            await favourites.RemoveAsync(favourite.Id, cancellationToken);
            await messages.CommitAsync(cancellationToken);
        }

        return await SummaryAsync(recipe, cancellationToken);
    }

    /// <summary>A recipe as a list row, with who has it as a favourite and how it is rated.</summary>
    private async Task<RecipeSummaryResponse> SummaryAsync(Recipe recipe, CancellationToken cancellationToken) =>
        recipe.ToSummary((await FavouredByAsync([recipe], cancellationToken))(recipe), (await RatedAsync([recipe], cancellationToken))(recipe));

    /// <summary>Loads the favourites of <paramref name="shown"/> at once, and returns who has each one.</summary>
    private async Task<Func<Recipe, IReadOnlyList<FavouriteResponse>>> FavouredByAsync(
        IReadOnlyCollection<Recipe> shown, CancellationToken cancellationToken)
    {
        var byRecipe = (await favourites.ListForRecipesAsync(shown.Select(recipe => recipe.Id).ToList(), cancellationToken))
            .ToLookup(favourite => favourite.RecipeId, favourite => new FavouriteResponse(favourite.UserId, favourite.UserName));
        return recipe => byRecipe[recipe.Id].ToList();
    }
}
