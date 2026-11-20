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
}
