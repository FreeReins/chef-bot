using ChefBot.Core.Engines;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeRecipes : IRecipeAccess
{
    public List<Recipe> Rows { get; } = [];

    /// <summary>Stored photo bytes, by photo id.</summary>
    public Dictionary<Guid, RecipeImageFile> ImageFiles { get; } = [];

    public Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(r => !r.IsArchived).OrderBy(r => r.Name).Select(Copies.Of).ToList());

    public Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken) => Task.FromResult(Rows.Select(Copies.Of).ToList());

    public Task<RecipeSearchResult> SearchActiveAsync(RecipeQuery query, CancellationToken cancellationToken)
    {
        var matching = Rows
            .Where(r => !r.IsArchived && (query.Course is null || r.Course == query.Course))
            .Where(r => query.Words.All(word => Contains(r.Name, word)
                || r.Ingredients.Any(i => Contains(i.Text, word) || Contains(i.DisplayName, word))
                || r.Tags.Any(tag => Contains(tag, word))))
            .Where(r => (query.Tags ?? []).All(wanted => r.Tags.Any(tag => string.Equals(tag.Trim(), wanted.Trim(), StringComparison.OrdinalIgnoreCase))))
            .Where(r => query.OnlyIds is null || query.OnlyIds.Contains(r.Id))
            .Where(r => !query.SlowCooker || r.IsSlowCooker)
            .Where(r => query.ReadyWithin is not int within || RecipeMatcher.MinutesOf(r) is int minutes && minutes <= within)
            .OrderBy(r => r.Course)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ThenBy(r => r.Id)
            .ToList();
        return Task.FromResult(new RecipeSearchResult(
            matching.Skip(query.Skip).Take(query.Take).Select(Copies.Of).ToList(), matching.Count));
    }

    public Task<List<string>> ListActiveTagsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(r => !r.IsArchived).SelectMany(r => r.Tags).ToList());

    public Task<int> CountActiveAsync(CancellationToken cancellationToken) => Task.FromResult(Rows.Count(r => !r.IsArchived));

    public Task<Recipe?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(r => r.Id == id) is { } row ? Copies.Of(row) : null);

    public Task SaveAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(r => r.Id == recipe.Id);
        Rows.Add(Copies.Of(recipe));

        // Like the database, a photo taken off its recipe loses its bytes.
        var kept = Rows.SelectMany(r => r.Images).Select(image => image.Id).ToHashSet();
        foreach (var gone in ImageFiles.Keys.Where(id => !kept.Contains(id)).ToList())
        {
            ImageFiles.Remove(gone);
        }

        return Task.CompletedTask;
    }

    public Task<RecipeImageFile?> GetImageFileAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Any(r => r.Id == recipeId && r.Images.Any(image => image.Id == imageId && image.IsStored))
            ? ImageFiles.GetValueOrDefault(imageId)
            : null);

    public Task SaveImageFileAsync(RecipeImageFile file, CancellationToken cancellationToken)
    {
        ImageFiles[file.ImageId] = file;
        return Task.CompletedTask;
    }

    private static bool Contains(string text, string word) => text.Contains(word, StringComparison.OrdinalIgnoreCase);
}
