using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>Recipe health: how complete the library is.</content>
internal sealed partial class RecipeManager
{
    public async Task<RecipeHealthResponse> GetHealthAsync(Guid? cookbookId, CancellationToken cancellationToken)
    {
        // A cookbook's report looks only at its recipes: their checks, and names and tags shared among them.
        var cookbook = cookbookId is Guid id ? await validation.CookbookAsync(id, cancellationToken) : null;
        var library = (await recipes.ListActiveAsync(cancellationToken))
            .Where(recipe => cookbook is null || recipe.CookbookIds.Contains(cookbook.Id))
            .ToList();
        var issues = library.ToDictionary(recipe => recipe.Id, RecipeHealth.Issues);
        var checks = RecipeHealth.All
            .Select(issue => new RecipeHealthCheck(issue, library.Where(recipe => issues[recipe.Id].Contains(issue)).Select(Ref).ToList()))
            .ToList();
        var duplicates = RecipeHealth.Duplicates(library).Select(group => new RecipeDuplicateGroup(group.Select(Ref).ToList())).ToList();
        var rareTags = RecipeHealth.RareTags(library).Select(used => new RecipeRareTag(used.Tag, Ref(used.Recipe))).ToList();
        return new RecipeHealthResponse(
            library.Count,
            issues.Values.Count(found => found.Count == 0),
            checks,
            duplicates,
            rareTags,
            cookbook is null ? null : new RecipeCookbookResponse(cookbook.Id, cookbook.Name));
    }

    private static RecipeRef Ref(Recipe recipe) => new(recipe.Id, recipe.Name);
}
