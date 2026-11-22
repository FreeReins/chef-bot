using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IRecipeAccess"/>
internal sealed class RecipeAccess(ChefBotDbContext db) : IRecipeAccess
{
    /// <summary>The character that makes the next <c>%</c>, <c>_</c> or itself in a LIKE pattern match literally.</summary>
    private const string LikeEscape = "\\";

    private IQueryable<RecipeEntity> WithIngredients => db.Recipes.Include(recipe => recipe.Ingredients).Include(recipe => recipe.Images).AsSplitQuery();

    public async Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken) =>
        (await WithIngredients.Where(recipe => !recipe.IsArchived).OrderBy(recipe => recipe.Name).ToListAsync(cancellationToken))
            .Select(recipe => recipe.ToContract()).ToList();

    public async Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken) =>
        (await WithIngredients.ToListAsync(cancellationToken)).Select(recipe => recipe.ToContract()).ToList();

    public async Task<RecipeSearchResult> SearchActiveAsync(RecipeQuery query, CancellationToken cancellationToken)
    {
        var matching = db.Recipes.AsNoTracking().Where(recipe => !recipe.IsArchived);
        if (query.Course is RecipeCourse course)
        {
            matching = matching.Where(recipe => recipe.Course == course);
        }

        foreach (var tag in query.Tags ?? [])
        {
            var exact = EscapeLike(tag.Trim());
            matching = matching.Where(recipe => recipe.Tags.Any(saved => EF.Functions.ILike(saved, exact, LikeEscape)));
        }

        if (query.OnlyIds is { } ids)
        {
            matching = matching.Where(recipe => ids.Contains(recipe.Id));
        }

        if (query.SlowCooker)
        {
            matching = matching.Where(recipe => recipe.IsSlowCooker);
        }

        // As RecipeMatcher.MinutesOf: the total time when there is one, or else prep and cook time added up.
        if (query.ReadyWithin is int within)
        {
            matching = matching.Where(recipe => recipe.TotalMinutes > 0
                ? recipe.TotalMinutes <= within
                : (recipe.PrepMinutes != null || recipe.CookMinutes != null) && (recipe.PrepMinutes ?? 0) + (recipe.CookMinutes ?? 0) <= within);
        }

        foreach (var word in query.Words)
        {
            var pattern = $"%{EscapeLike(word)}%";
            matching = matching.Where(recipe =>
                EF.Functions.ILike(recipe.Name, pattern, LikeEscape)
                || recipe.Ingredients.Any(ingredient => EF.Functions.ILike(ingredient.Text, pattern, LikeEscape)
                    || EF.Functions.ILike(ingredient.DisplayName, pattern, LikeEscape))
                || recipe.Tags.Any(tag => EF.Functions.ILike(tag, pattern, LikeEscape)));
        }

        var total = await matching.CountAsync(cancellationToken);
        var page = await matching
            .OrderBy(recipe => recipe.Course == RecipeCourse.Main ? 0
                : recipe.Course == RecipeCourse.Breakfast ? 1
                : recipe.Course == RecipeCourse.Snack ? 2
                : recipe.Course == RecipeCourse.Sweet ? 3
                : recipe.Course == RecipeCourse.Drink ? 4
                : 5)
            .ThenBy(recipe => recipe.Name)
            .ThenBy(recipe => recipe.Id)
            .Skip(query.Skip)
            .Take(query.Take)
            .Include(recipe => recipe.Ingredients)
            .Include(recipe => recipe.Images)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        return new RecipeSearchResult(page.Select(recipe => recipe.ToContract()).ToList(), total);
    }

    public async Task<List<string>> ListActiveTagsAsync(CancellationToken cancellationToken) =>
        (await db.Recipes.AsNoTracking().Where(recipe => !recipe.IsArchived).Select(recipe => recipe.Tags).ToListAsync(cancellationToken))
        .SelectMany(tags => tags)
        .ToList();

    public Task<int> CountActiveAsync(CancellationToken cancellationToken) =>
        db.Recipes.CountAsync(recipe => !recipe.IsArchived, cancellationToken);

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
        db.SyncChildren(tracked.Images, fresh.Images, image => image.Id);
    }

    public async Task<RecipeImageFile?> GetImageFileAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken)
    {
        var found = await db.Set<RecipeImageEntity>().AsNoTracking()
            .Where(image => image.Id == imageId && image.RecipeId == recipeId && image.ContentType != null)
            .Join(db.RecipeImageData, image => image.Id, data => data.ImageId, (image, data) => new { image.ContentType, data.Data })
            .FirstOrDefaultAsync(cancellationToken);
        return found is null ? null : new RecipeImageFile(imageId, found.ContentType!, found.Data);
    }

    public async Task SaveImageFileAsync(RecipeImageFile file, CancellationToken cancellationToken)
    {
        var tracked = db.RecipeImageData.Local.FirstOrDefault(data => data.ImageId == file.ImageId)
            ?? await db.RecipeImageData.FirstOrDefaultAsync(data => data.ImageId == file.ImageId, cancellationToken);
        if (tracked is null)
        {
            db.RecipeImageData.Add(new RecipeImageDataEntity { ImageId = file.ImageId, Data = file.Data });
        }
        else
        {
            tracked.Data = file.Data;
        }
    }

    private async Task<RecipeEntity?> TrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.Recipes.Local.FirstOrDefault(recipe => recipe.Id == id)
        ?? await WithIngredients.FirstOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);

    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
