namespace ChefBot.Core.Utilities;

/// <summary>How an ingredient is identified across recipes, shopping lists and the pantry.</summary>
internal interface IIngredientRef
{
    /// <summary>The exact item, used to merge shopping lines and match pantry stock ("chicken thigh").</summary>
    string Key { get; }

    /// <summary>The catalog entry it belongs to, when known ("chicken").</summary>
    string? CatalogName { get; }

    /// <summary>The name shown to people.</summary>
    string DisplayName { get; }
}
