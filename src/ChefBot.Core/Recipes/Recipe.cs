using System.Text.Json.Serialization;
using ChefBot.Core.Ingredients;

namespace ChefBot.Core.Recipes;

/// <summary>What part of the week's eating a recipe is for.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeCourse>))]
public enum RecipeCourse
{
    /// <summary>A dinner, which may also feed the next days' lunches.</summary>
    Main,
    Breakfast,
    Snack,
    Sweet,

    /// <summary>Sides, drinks, sauces and anything else that is never planned on its own.</summary>
    Other,
}

/// <summary>Where a recipe came from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeOrigin>))]
public enum RecipeOrigin
{
    /// <summary>Entered or edited in ChefBot. Imports never overwrite it.</summary>
    Local,

    /// <summary>Imported from Mealie and refreshed by the next import.</summary>
    Mealie,
}

/// <summary>A recipe stored in ChefBot's own database.</summary>
public sealed class Recipe
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public RecipeOrigin Origin { get; set; }

    /// <summary>The id in the system it was imported from, if any.</summary>
    public string? ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Servings { get; set; } = 4;

    public RecipeCourse Course { get; set; }

    public bool IsSlowCooker { get; set; }

    /// <summary>Archived recipes stay on past plans but are never planned again.</summary>
    public bool IsArchived { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<RecipeIngredient> Ingredients { get; set; } = [];

    /// <summary>The catalog ingredients this recipe uses, each once.</summary>
    public IReadOnlyList<string> GetCatalogNames() =>
        Ingredients.Where(i => i.CatalogName is not null).Select(i => i.CatalogName!).Distinct().ToList();

    /// <summary>Swaps in a new ingredient list, leaving the stored rows alone when nothing changed.</summary>
    public void ReplaceIngredients(IReadOnlyList<RecipeIngredient> fresh)
    {
        var current = Ingredients.OrderBy(i => i.Position).Select(Signature);
        if (current.SequenceEqual(fresh.OrderBy(i => i.Position).Select(Signature)))
        {
            return;
        }

        Ingredients.Clear();
        Ingredients.AddRange(fresh);
    }

    private static (string, string?, string, decimal?, string) Signature(RecipeIngredient ingredient) =>
        (ingredient.Key, ingredient.CatalogName, ingredient.DisplayName, ingredient.Quantity, ingredient.Unit);
}

/// <summary>One ingredient line of a recipe.</summary>
public sealed class RecipeIngredient : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RecipeId { get; set; }

    /// <summary>The line's place in the recipe.</summary>
    public int Position { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The amount for the recipe as written, or null when the recipe doesn't say.</summary>
    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;
}
