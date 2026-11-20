namespace ChefBot.Core.Utilities;

/// <summary>The result of looking a piece of ingredient text up in the catalog.</summary>
internal sealed record IngredientIdentity(string Key, string? CatalogName, string DisplayName) : IIngredientRef;
