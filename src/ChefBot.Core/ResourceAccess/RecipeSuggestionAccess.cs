using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IRecipeSuggestionAccess"/>
internal sealed class RecipeSuggestionAccess(ChefBotDbContext db) : IRecipeSuggestionAccess
{
    public async Task<RecipeSuggestion?> FindAsync(Guid recipeId, CancellationToken cancellationToken) =>
        (await TrackedAsync(recipeId, cancellationToken))?.ToContract();

    public async Task<List<RecipeSuggestion>> ListForRecipesAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken) =>
        recipeIds.Count == 0
            ? []
            : (await db.RecipeSuggestions.AsNoTracking().Where(suggestion => recipeIds.Contains(suggestion.RecipeId)).ToListAsync(cancellationToken))
                .Select(suggestion => suggestion.ToContract())
                .ToList();

    public async Task<List<RecipeSuggestion>> ListAllAsync(CancellationToken cancellationToken) =>
        (await db.RecipeSuggestions.AsNoTracking().ToListAsync(cancellationToken)).Select(suggestion => suggestion.ToContract()).ToList();

    public async Task SaveAsync(RecipeSuggestion suggestion, CancellationToken cancellationToken)
    {
        var fresh = suggestion.ToEntity();
        if (await TrackedAsync(suggestion.RecipeId, cancellationToken) is { } row)
        {
            fresh.Id = row.Id;
            db.CopyInto(row, fresh);
        }
        else
        {
            db.RecipeSuggestions.Add(fresh);
        }
    }

    public async Task RemoveAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        if (await TrackedAsync(recipeId, cancellationToken) is { } row)
        {
            db.RecipeSuggestions.Remove(row);
        }
    }

    /// <summary>The recipe's row, also when it was added earlier in this request.</summary>
    private async Task<RecipeSuggestionEntity?> TrackedAsync(Guid recipeId, CancellationToken cancellationToken) =>
        db.RecipeSuggestions.Local.FirstOrDefault(suggestion => suggestion.RecipeId == recipeId)
        ?? await db.RecipeSuggestions.FirstOrDefaultAsync(suggestion => suggestion.RecipeId == recipeId, cancellationToken);
}
