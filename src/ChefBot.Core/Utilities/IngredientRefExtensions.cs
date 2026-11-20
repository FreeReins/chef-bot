namespace ChefBot.Core.Utilities;

internal static class IngredientRefExtensions
{
    /// <summary>The loosest sensible name for "do I have something like this": the catalog name, else the key.</summary>
    public static string MatchName(this IIngredientRef ingredient) => ingredient.CatalogName ?? ingredient.Key;
}
