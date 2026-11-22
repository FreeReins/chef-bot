using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>
/// Cookbooks: named groups of recipes. A recipe can be in any number of them, or none, and which ones is kept on the
/// recipe. Removing a cookbook leaves its recipes in the library.
/// </content>
internal sealed partial class RecipeManager
{
    public async Task<IReadOnlyList<CookbookResponse>> ListCookbooksAsync(CancellationToken cancellationToken)
    {
        var counts = await recipes.CountActiveByCookbookAsync(cancellationToken);
        return (await cookbooks.ListAsync(cancellationToken))
            .Select(cookbook => cookbook.ToResponse(counts.GetValueOrDefault(cookbook.Id)))
            .ToList();
    }

    public async Task<CookbookResponse> CreateCookbookAsync(CookbookDraft draft, CancellationToken cancellationToken)
    {
        var (name, description) = await validation.CookbookDraftAsync(draft, renaming: null, cancellationToken);
        var cookbook = new Cookbook { Name = name, Description = description, UpdatedAt = clock.GetUtcNow() };
        await cookbooks.SaveAsync(cookbook, cancellationToken);
        await messages.PublishAsync(new CookbooksChanged());
        await messages.CommitAsync(cancellationToken);
        return cookbook.ToResponse(0);
    }

    public async Task<CookbookResponse> UpdateCookbookAsync(Guid id, CookbookDraft draft, CancellationToken cancellationToken)
    {
        var cookbook = await validation.CookbookAsync(id, cancellationToken);
        (cookbook.Name, cookbook.Description) = await validation.CookbookDraftAsync(draft, renaming: id, cancellationToken);
        cookbook.UpdatedAt = clock.GetUtcNow();
        await cookbooks.SaveAsync(cookbook, cancellationToken);
        await messages.PublishAsync(new CookbooksChanged());
        await messages.CommitAsync(cancellationToken);
        return await CookbookResponseAsync(cookbook, cancellationToken);
    }

    public async Task RemoveCookbookAsync(Guid id, CancellationToken cancellationToken)
    {
        var cookbook = await cookbooks.FindAsync(id, cancellationToken);
        var members = await recipes.ListInCookbookAsync(id, cancellationToken);
        if (cookbook is null && members.Count == 0)
        {
            return;
        }

        foreach (var recipe in members)
        {
            recipe.CookbookIds.RemoveAll(cookbookId => cookbookId == id);
            await recipes.SaveAsync(recipe, cancellationToken);
        }

        await cookbooks.RemoveAsync(id, cancellationToken);
        await messages.PublishAsync(new CookbookRemoved(id)); // plan style rules stop naming it, in the background
        await messages.PublishAsync(new CookbooksChanged());
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<CookbookResponse> AddToCookbookAsync(Guid id, CookbookRecipesRequest request, CancellationToken cancellationToken)
    {
        var cookbook = await validation.CookbookAsync(id, cancellationToken);
        var chosen = await validation.RecipesForCookbookAsync(request?.RecipeIds, cancellationToken);
        var added = 0;
        foreach (var recipe in chosen.Where(recipe => !recipe.CookbookIds.Contains(id)))
        {
            recipe.CookbookIds.Add(id);
            await recipes.SaveAsync(recipe, cancellationToken);
            added++;
        }

        if (added > 0)
        {
            await messages.PublishAsync(new CookbooksChanged());
            await messages.CommitAsync(cancellationToken);
        }

        return await CookbookResponseAsync(cookbook, cancellationToken);
    }

    public async Task<CookbookResponse> RemoveFromCookbookAsync(Guid id, Guid recipeId, CancellationToken cancellationToken)
    {
        var cookbook = await validation.CookbookAsync(id, cancellationToken);
        var recipe = await validation.RecipeAsync(recipeId, cancellationToken);
        if (recipe.CookbookIds.RemoveAll(cookbookId => cookbookId == id) > 0)
        {
            await recipes.SaveAsync(recipe, cancellationToken);
            await messages.PublishAsync(new CookbooksChanged());
            await messages.CommitAsync(cancellationToken);
        }

        return await CookbookResponseAsync(cookbook, cancellationToken);
    }

    public async Task<RecipeExportFile> ExportCookbookAsync(Guid id, RecipeExportFormat format, CancellationToken cancellationToken)
    {
        var cookbook = await validation.CookbookAsync(id, cancellationToken);
        var members = (await recipes.ListInCookbookAsync(id, cancellationToken)).Where(recipe => !recipe.IsArchived);
        return TransformationEngine.ExportFile(members, cookbook, format);
    }

    /// <summary>Every cookbook's name, by id, to name the cookbooks recipes are in.</summary>
    private async Task<IReadOnlyDictionary<Guid, string>> CookbookNamesAsync(CancellationToken cancellationToken) =>
        (await cookbooks.ListAsync(cancellationToken)).ToDictionary(cookbook => cookbook.Id, cookbook => cookbook.Name);

    /// <summary>The cookbook imported recipes go in, as a list for <see cref="Recipe.CookbookIds"/>: empty for none.</summary>
    /// <exception cref="Contracts.NotFoundException">The cookbook doesn't exist.</exception>
    private async Task<List<Guid>> ImportCookbookAsync(Guid? cookbookId, CancellationToken cancellationToken) =>
        cookbookId is Guid id ? [(await validation.CookbookAsync(id, cancellationToken)).Id] : [];

    private async Task<CookbookResponse> CookbookResponseAsync(Cookbook cookbook, CancellationToken cancellationToken) =>
        cookbook.ToResponse((await recipes.CountActiveByCookbookAsync(cancellationToken)).GetValueOrDefault(cookbook.Id));
}
