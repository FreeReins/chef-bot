using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeRecipes : IRecipeAccess
{
    public List<Recipe> Rows { get; } = [];

    public Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(r => !r.IsArchived).OrderBy(r => r.Name).Select(Copies.Of).ToList());

    public Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken) => Task.FromResult(Rows.Select(Copies.Of).ToList());

    public Task<Recipe?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(r => r.Id == id) is { } row ? Copies.Of(row) : null);

    public Task SaveAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(r => r.Id == recipe.Id);
        Rows.Add(Copies.Of(recipe));
        return Task.CompletedTask;
    }
}
