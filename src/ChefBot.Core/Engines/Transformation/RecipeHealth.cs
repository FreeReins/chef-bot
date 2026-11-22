using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// How complete recipes are: what one recipe is missing, and across the library, recipes that share a name and tags
/// only one recipe uses. Plain functions over recipes already loaded, so they cost no extra reads.
/// </summary>
internal static class RecipeHealth
{
    /// <summary>Every issue, in the order a report lists them.</summary>
    public static IReadOnlyList<RecipeIssue> All { get; } = Enum.GetValues<RecipeIssue>();

    /// <summary>Words on an ingredient line that mean it needs no amount, such as "salt, to taste".</summary>
    private static readonly string[] NoAmountNeeded = ["taste", "needed", "serving", "garnish", "optional", "pinch", "dash", "spray", "drizzle"];

    /// <summary>What the recipe is missing, in <see cref="All"/> order: empty when it is complete.</summary>
    public static IReadOnlyList<RecipeIssue> Issues(Recipe recipe)
    {
        var issues = new List<RecipeIssue>();
        if (recipe.Images.Count == 0) issues.Add(RecipeIssue.NoPhoto);
        if (string.IsNullOrWhiteSpace(recipe.Description)) issues.Add(RecipeIssue.NoDescription);
        if (recipe.PrepMinutes is null && recipe.CookMinutes is null && recipe.TotalMinutes is null) issues.Add(RecipeIssue.NoTimes);
        if (recipe.Tags.Count == 0) issues.Add(RecipeIssue.NoTags);
        if (StepsAreThin(recipe.Instructions)) issues.Add(RecipeIssue.ThinSteps);
        if (LinesWithoutAmounts(recipe).Count > 0) issues.Add(RecipeIssue.MissingAmounts);
        return issues;
    }

    /// <summary>True when the steps are missing or too short to cook from.</summary>
    public static bool StepsAreThin(IReadOnlyList<string> steps)
    {
        var words = steps.Sum(step => step.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        return steps.Count == 0 || words < 25 || (steps.Count == 1 && words < 40);
    }

    /// <summary>The positions (from 0) of ingredient lines with no amount that need one.</summary>
    public static List<int> LinesWithoutAmounts(Recipe recipe) =>
        Lines(recipe)
            .Select((line, index) => (line, index))
            .Where(item => IngredientLineParser.Parse(item.line).Quantity is null
                && !NoAmountNeeded.Any(word => item.line.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Select(item => item.index)
            .ToList();

    /// <summary>The ingredient lines as written, in order.</summary>
    public static List<string> Lines(Recipe recipe) =>
        recipe.Ingredients.OrderBy(ingredient => ingredient.Position).Select(ingredient => ingredient.Text).ToList();

    /// <summary>Recipes whose names match once case, accents and punctuation are ignored, each group by name.</summary>
    public static IReadOnlyList<IReadOnlyList<Recipe>> Duplicates(IEnumerable<Recipe> recipes) =>
        recipes
            .Where(recipe => TextNormalizer.Normalize(recipe.Name).Length > 0)
            .GroupBy(recipe => TextNormalizer.Normalize(recipe.Name))
            .Where(group => group.Count() > 1)
            .Select(group => (IReadOnlyList<Recipe>)[.. group.OrderBy(recipe => recipe.Name, StringComparer.OrdinalIgnoreCase)])
            .OrderBy(group => group[0].Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Tags exactly one recipe uses, ignoring case, spelled as that recipe spells them, with the recipe.</summary>
    public static IReadOnlyList<(string Tag, Recipe Recipe)> RareTags(IEnumerable<Recipe> recipes) =>
        recipes
            .SelectMany(recipe => recipe.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(tag => (Tag: tag, Recipe: recipe)))
            .GroupBy(used => used.Tag, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .Select(group => group.First())
            .OrderBy(used => used.Tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
