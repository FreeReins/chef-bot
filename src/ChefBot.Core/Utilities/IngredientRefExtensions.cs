namespace ChefBot.Core.Utilities;

internal static class IngredientRefExtensions
{
    /// <summary>The loosest sensible name for "do I have something like this": the catalog name, else the key.</summary>
    public static string MatchName(this IIngredientRef ingredient) => ingredient.CatalogName ?? ingredient.Key;

    /// <summary>
    /// The general kind a variety belongs to, so a substitute is easy to find: "onion" for shallot. Null when the
    /// ingredient already is that kind, or isn't in the catalog.
    /// </summary>
    public static string? OrInstead(this IIngredientRef ingredient) =>
        ingredient.CatalogName is { } name && !string.Equals(name, ingredient.Key, StringComparison.Ordinal) ? name : null;
}
