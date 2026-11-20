using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.ResourceAccess.Contracts;

/// <summary>One entry on a weekly plan: a recipe, leftovers, or a free-form meal built from named ingredients.</summary>
internal sealed class PlannedMeal
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The suggested day. Snacks and sweets have none: make them whenever suits.</summary>
    public DateOnly? Date { get; set; }

    public MealSlot Slot { get; set; }

    public Guid? RecipeId { get; set; }

    /// <summary>The recipe as it was loaded with the plan. Saving a plan only stores <see cref="RecipeId"/>.</summary>
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

    /// <summary>What goes into this meal, whichever way it is defined.</summary>
    public IReadOnlyList<IIngredientRef> Ingredients => Recipe is not null ? Recipe.Ingredients : CustomIngredients;
}
