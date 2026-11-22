using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>
/// Decides whether a recipe matches a plan style's rule, and describes rules in plain words for the plan's notes.
/// Plain functions shared by the planner, not an engine.
/// </summary>
internal static class RecipeMatcher
{
    /// <param name="recipe">The recipe.</param>
    /// <param name="rule">The rule.</param>
    /// <param name="fans">How many people have each recipe as a favourite, for <see cref="RecipeMatchKind.Favourite"/>.</param>
    public static bool Matches(Recipe recipe, PlanRule rule, IReadOnlyDictionary<Guid, int>? fans = null) => rule.Match switch
    {
        RecipeMatchKind.Favourite => fans?.GetValueOrDefault(recipe.Id) > 0,
        RecipeMatchKind.SlowCooker => recipe.IsSlowCooker,
        RecipeMatchKind.Tag => recipe.Tags.Any(tag => rule.Values.Any(value => SameWords(tag, value))),
        RecipeMatchKind.Ingredient => recipe.Ingredients.Any(ingredient => rule.Values.Any(value => Uses(ingredient, value))),
        RecipeMatchKind.ReadyWithin => MinutesOf(recipe) is int minutes && minutes <= (rule.Minutes ?? 0),
        RecipeMatchKind.Cookbook => recipe.CookbookIds.Any(id => rule.Values.Contains(id.ToString(), StringComparer.OrdinalIgnoreCase)),
        _ => false,
    };

    /// <summary>How long a recipe takes from start to finish, or null when it doesn't say.</summary>
    public static int? MinutesOf(Recipe recipe) =>
        recipe.TotalMinutes is > 0
            ? recipe.TotalMinutes
            : recipe.PrepMinutes is null && recipe.CookMinutes is null ? null : (recipe.PrepMinutes ?? 0) + (recipe.CookMinutes ?? 0);

    /// <summary>What the rule looks for, to follow "are": "slow cooker", "made with chicken or beef".</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="cookbookNames">Every cookbook's name, by id, for <see cref="RecipeMatchKind.Cookbook"/>.</param>
    public static string Describe(PlanRule rule, IReadOnlyDictionary<Guid, string>? cookbookNames = null) => rule.Match switch
    {
        RecipeMatchKind.Cookbook => InCookbooks(rule.Values, cookbookNames),
        RecipeMatchKind.SlowCooker => "slow cooker",
        RecipeMatchKind.Tag => $"tagged {Either(rule.Values)}",
        RecipeMatchKind.Ingredient => $"made with {Either(rule.Values)}",
        RecipeMatchKind.ReadyWithin => $"ready within {rule.Minutes} minutes",
        RecipeMatchKind.Favourite => "someone's favourite",
        _ => "matching",
    };

    /// <summary>The kind of meal a rule counts, in the plural: "dinners", or "meals" for every kind.</summary>
    public static string Plural(MealSlot? slot) => slot switch
    {
        null => "meals",
        MealSlot.Lunch => "lunches",
        _ => $"{slot.Value.ToString().ToLowerInvariant()}s",
    };

    /// <summary>
    /// True when the ingredient is, or is a kind of, <paramref name="value"/>: "chicken" matches chicken thighs and
    /// chicken broth. Whole words only, so "ham" doesn't match "graham crackers".
    /// </summary>
    private static bool Uses(IIngredientRef ingredient, string value) =>
        SameWords(ingredient.CatalogName, value) || ContainsWords(ingredient.Key, value) || ContainsWords(ingredient.DisplayName, value);

    private static bool SameWords(string? text, string value) =>
        text is not null && TextNormalizer.Normalize(text) == TextNormalizer.Normalize(value);

    private static bool ContainsWords(string? text, string value)
    {
        var words = TextNormalizer.Normalize(value);
        return words.Length > 0 && $" {TextNormalizer.Normalize(text)} ".Contains($" {words} ", StringComparison.Ordinal);
    }

    /// <summary>"in the Weeknight cookbook", "in the Weeknight or Grandma's cookbooks".</summary>
    private static string InCookbooks(IReadOnlyList<string> ids, IReadOnlyDictionary<Guid, string>? names)
    {
        var named = ids
            .Select(id => Guid.TryParse(id, out var guid) && names?.GetValueOrDefault(guid) is { } name ? name : null)
            .OfType<string>()
            .ToList();
        return named.Count switch
        {
            0 => "in a cookbook that has been removed",
            1 => $"in the {named[0]} cookbook",
            _ => $"in the {Either(named)} cookbooks",
        };
    }

    /// <summary>"chicken", "chicken or beef", or "chicken, beef or 4 more".</summary>
    private static string Either(IReadOnlyList<string> values) => values.Count switch
    {
        0 => "anything",
        1 => values[0],
        2 or 3 => $"{string.Join(", ", values.Take(values.Count - 1))} or {values[^1]}",
        _ => $"{values[0]}, {values[1]} or {values.Count - 2} more",
    };
}
