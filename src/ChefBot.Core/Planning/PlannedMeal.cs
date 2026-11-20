using System.Text.Json.Serialization;
using ChefBot.Core.Ingredients;
using ChefBot.Core.Recipes;

namespace ChefBot.Core.Planning;

[JsonConverter(typeof(JsonStringEnumConverter<MealSlot>))]
public enum MealSlot
{
    Breakfast,
    Lunch,
    Dinner,
    Snack,
    Sweet,
}

[JsonConverter(typeof(JsonStringEnumConverter<MealStatus>))]
public enum MealStatus
{
    Planned,
    Cooked,
    Skipped,
}

/// <summary>One entry on a weekly plan: a recipe, leftovers, or a free-form meal built from named ingredients.</summary>
public sealed class PlannedMeal
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid MealPlanId { get; set; }

    /// <summary>The suggested day. Snacks and sweets have none: make them whenever suits.</summary>
    public DateOnly? Date { get; set; }

    public MealSlot Slot { get; set; }

    public Guid? RecipeId { get; set; }

    public Recipe? Recipe { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Portions to cook. Zero means "make the recipe once, as written".</summary>
    public int Portions { get; set; }

    /// <summary>Optional meals are planned but their groceries are marked as skippable.</summary>
    public bool IsOptional { get; set; }

    /// <summary>The dinner this lunch is leftovers of, if it is leftovers.</summary>
    public Guid? LeftoverOfMealId { get; set; }

    public MealStatus Status { get; set; } = MealStatus.Planned;

    /// <summary>Ingredients of a free-form meal. Empty when the meal follows a recipe.</summary>
    public List<PlannedMealIngredient> CustomIngredients { get; set; } = [];

    public bool IsLeftover => LeftoverOfMealId is not null;

    public static PlannedMeal ForRecipe(Recipe recipe, MealSlot slot, DateOnly? date, int portions)
    {
        var meal = new PlannedMeal { Slot = slot, Date = date };
        meal.UseRecipe(recipe, portions);
        return meal;
    }

    public static PlannedMeal LeftoverOf(PlannedMeal dinner, DateOnly date) => new()
    {
        Slot = MealSlot.Lunch,
        Date = date,
        RecipeId = dinner.RecipeId,
        Recipe = dinner.Recipe,
        Title = $"Leftovers: {dinner.Title}",
        LeftoverOfMealId = dinner.Id,
    };

    /// <summary>Makes this entry a fresh cooking of <paramref name="recipe"/>.</summary>
    public void UseRecipe(Recipe recipe, int portions)
    {
        Recipe = recipe;
        RecipeId = recipe.Id;
        Title = recipe.Name;
        CustomIngredients.Clear();
        ResetAsFreshMeal(portions);
    }

    /// <summary>Makes this entry a free-form meal, such as a grain, protein and vegetable bowl.</summary>
    public void UseCustom(string title, IEnumerable<PlannedMealIngredient> ingredients, int portions)
    {
        Recipe = null;
        RecipeId = null;
        Title = title;
        CustomIngredients.Clear();
        CustomIngredients.AddRange(ingredients);
        ResetAsFreshMeal(portions);
    }

    /// <summary>What goes into this meal, whichever way it is defined.</summary>
    public IReadOnlyList<IIngredientRef> GetIngredients()
    {
        if (Recipe is not null)
        {
            return Recipe.Ingredients;
        }

        return CustomIngredients;
    }

    private void ResetAsFreshMeal(int portions)
    {
        Portions = portions;
        LeftoverOfMealId = null;
        Status = MealStatus.Planned;
    }
}

/// <summary>An ingredient of a free-form meal. Amounts aren't tracked for these.</summary>
public sealed class PlannedMealIngredient : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid PlannedMealId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}
