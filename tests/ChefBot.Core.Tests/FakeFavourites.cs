using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>Favourites kept in a list. Names come from <paramref name="accounts"/>, as the database joins them.</summary>
internal sealed class FakeFavourites(FakeAccounts accounts) : IFavouriteAccess
{
    public List<RecipeFavourite> Rows { get; } = [];

    public Task<List<RecipeFavourite>> ListForRecipesAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken) =>
        Task.FromResult(Rows
            .Where(row => recipeIds.Contains(row.RecipeId))
            .Select(row => (Row: row, User: accounts.Users.FirstOrDefault(user => user.Id == row.UserId)))
            .Where(pair => pair.User is not null)
            .OrderBy(pair => pair.Row.CreatedAt)
            .Select(pair => Named(pair.Row, pair.User!.DisplayName))
            .ToList());

    public Task<List<RecipeFavourite>> ListAllAsync(CancellationToken cancellationToken) =>
        ListForRecipesAsync(Rows.Select(row => row.RecipeId).ToList(), cancellationToken);

    public Task<RecipeFavourite?> FindAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(row => row.RecipeId == recipeId && row.UserId == userId) is { } row
            ? Named(row, string.Empty)
            : null);

    public Task AddAsync(RecipeFavourite favourite, CancellationToken cancellationToken)
    {
        Rows.Add(Named(favourite, string.Empty));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == id);
        return Task.CompletedTask;
    }

    public Task RemoveForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.UserId == userId);
        return Task.CompletedTask;
    }

    private static RecipeFavourite Named(RecipeFavourite row, string name) => new()
    {
        Id = row.Id, RecipeId = row.RecipeId, UserId = row.UserId, CreatedAt = row.CreatedAt, UserName = name,
    };
}
