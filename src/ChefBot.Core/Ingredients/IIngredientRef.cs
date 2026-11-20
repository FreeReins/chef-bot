namespace ChefBot.Core.Ingredients;

/// <summary>
/// How an ingredient is identified across recipes, shopping lists and the pantry.
/// </summary>
public interface IIngredientRef
{
    /// <summary>The exact item, used to merge shopping lines and match pantry stock ("chicken thigh").</summary>
    string Key { get; }

    /// <summary>The catalog entry it belongs to, when known ("chicken").</summary>
    string? CatalogName { get; }

    /// <summary>The name shown to people.</summary>
    string DisplayName { get; }
}

/// <summary>The result of looking a piece of ingredient text up in the catalog.</summary>
public sealed record IngredientIdentity(string Key, string? CatalogName, string DisplayName) : IIngredientRef;

public static class IngredientRefExtensions
{
    /// <summary>The loosest sensible name for "do I have something like this": the catalog name, else the key.</summary>
    public static string MatchName(this IIngredientRef ingredient) => ingredient.CatalogName ?? ingredient.Key;
}
