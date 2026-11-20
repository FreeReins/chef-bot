using ChefBot.Core.Contracts;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Tests;

/// <summary>A Mealie that returns the recipes it is given, or fails when it has none, and records the connection used.</summary>
internal sealed class FakeSource : IRecipeSourceAccess
{
    public List<SourceRecipe>? Recipes { get; set; }

    public string? UsedUrl { get; private set; }

    public string? UsedToken { get; private set; }

    public Task<RecipeSourceResult> FetchAllAsync(MealieSettings connection, CancellationToken cancellationToken)
    {
        UsedUrl = connection.Url;
        UsedToken = connection.ApiToken;
        return Recipes is null
            ? throw new RecipeSourceException("Not a real Mealie.")
            : Task.FromResult(new RecipeSourceResult(Recipes, 0));
    }
}
