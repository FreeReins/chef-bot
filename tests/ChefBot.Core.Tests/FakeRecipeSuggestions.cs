using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>Suggestions kept in a list, one per recipe at most.</summary>
internal sealed class FakeRecipeSuggestions : IRecipeSuggestionAccess
{
    public List<RecipeSuggestion> Rows { get; } = [];

    public Task<RecipeSuggestion?> FindAsync(Guid recipeId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(row => row.RecipeId == recipeId) is { } row ? Copies.Of(row) : null);

    public Task<List<RecipeSuggestion>> ListForRecipesAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(row => recipeIds.Contains(row.RecipeId)).Select(Copies.Of).ToList());

    public Task<List<RecipeSuggestion>> ListAllAsync(CancellationToken cancellationToken) => Task.FromResult(Rows.Select(Copies.Of).ToList());

    public Task SaveAsync(RecipeSuggestion suggestion, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.RecipeId == suggestion.RecipeId);
        Rows.Add(Copies.Of(suggestion));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.RecipeId == recipeId);
        return Task.CompletedTask;
    }
}
