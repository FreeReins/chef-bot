using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IFavouriteAccess"/>
internal sealed class FavouriteAccess(ChefBotDbContext db) : IFavouriteAccess
{
    public async Task<List<RecipeFavourite>> ListForRecipesAsync(
        IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken)
    {
        if (recipeIds.Count == 0)
        {
            return [];
        }

        // Loading them tracks them, so one added or removed earlier in this request is seen.
        await db.RecipeFavourites.Where(favourite => recipeIds.Contains(favourite.RecipeId)).LoadAsync(cancellationToken);
        var favourites = db.RecipeFavourites.Local.Where(favourite => recipeIds.Contains(favourite.RecipeId)).ToList();
        var userIds = favourites.Select(favourite => favourite.UserId).Distinct().ToList();
        var names = await db.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        return favourites
            .Where(favourite => names.ContainsKey(favourite.UserId))
            .OrderBy(favourite => favourite.CreatedAt)
            .ThenBy(favourite => favourite.Id)
            .Select(favourite => favourite.ToContract(names[favourite.UserId]))
            .ToList();
    }

    public async Task<List<RecipeFavourite>> ListAllAsync(CancellationToken cancellationToken)
    {
        var names = await db.Users.AsNoTracking().ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);
        await db.RecipeFavourites.LoadAsync(cancellationToken);
        return db.RecipeFavourites.Local
            .Where(favourite => names.ContainsKey(favourite.UserId))
            .OrderBy(favourite => favourite.CreatedAt)
            .ThenBy(favourite => favourite.Id)
            .Select(favourite => favourite.ToContract(names[favourite.UserId]))
            .ToList();
    }

    public async Task<RecipeFavourite?> FindAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken)
    {
        await db.RecipeFavourites.Where(favourite => favourite.RecipeId == recipeId && favourite.UserId == userId)
            .LoadAsync(cancellationToken);
        var found = db.RecipeFavourites.Local.FirstOrDefault(favourite => favourite.RecipeId == recipeId && favourite.UserId == userId);
        return found?.ToContract(string.Empty);
    }

    public Task AddAsync(RecipeFavourite favourite, CancellationToken cancellationToken)
    {
        db.RecipeFavourites.Add(favourite.ToEntity());
        return Task.CompletedTask;
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        await db.RecipeFavourites.Where(favourite => favourite.Id == id).LoadAsync(cancellationToken);
        db.RecipeFavourites.RemoveRange(db.RecipeFavourites.Local.Where(favourite => favourite.Id == id).ToList());
    }

    public async Task RemoveForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await db.RecipeFavourites.Where(favourite => favourite.UserId == userId).LoadAsync(cancellationToken);
        db.RecipeFavourites.RemoveRange(db.RecipeFavourites.Local.Where(favourite => favourite.UserId == userId).ToList());
    }
}
