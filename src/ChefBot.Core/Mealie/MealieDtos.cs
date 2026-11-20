using System.Text.Json.Serialization;

namespace ChefBot.Core.Mealie;

// The parts of Mealie's JSON that ChefBot reads. Everything is optional, so an
// unexpected or missing field leaves a gap instead of failing the whole import.

internal sealed class MealiePage
{
    public List<MealieSummary>? Items { get; set; }

    [JsonPropertyName("total_pages")]
    public int? TotalPagesSnakeCase { get; set; }

    public int? TotalPages { get; set; }

    [JsonIgnore]
    public int PageCount => TotalPagesSnakeCase ?? TotalPages ?? 1;
}

internal sealed class MealieSummary
{
    public string? Slug { get; set; }
}

internal sealed class MealieRecipe
{
    public string? Id { get; set; }

    public string? Slug { get; set; }

    public string? Name { get; set; }

    public decimal? RecipeServings { get; set; }

    public decimal? RecipeYieldQuantity { get; set; }

    public List<MealieNamed>? Tags { get; set; }

    public List<MealieNamed>? RecipeCategory { get; set; }

    public List<MealieNamed>? Tools { get; set; }

    public List<MealieIngredient>? RecipeIngredient { get; set; }
}

internal sealed class MealieIngredient
{
    public decimal? Quantity { get; set; }

    public MealieNamed? Unit { get; set; }

    public MealieNamed? Food { get; set; }

    public string? Note { get; set; }

    public string? OriginalText { get; set; }

    public string? Display { get; set; }
}

internal sealed class MealieNamed
{
    public string? Name { get; set; }
}
