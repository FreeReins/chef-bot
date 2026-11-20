using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class PlannedMealEntity
{
    public Guid Id { get; set; }

    public Guid MealPlanId { get; set; }

    public DateOnly? Date { get; set; }

    public MealSlot Slot { get; set; }

    public Guid? RecipeId { get; set; }

    /// <summary>Loaded with a plan. Never written through: a meal only stores <see cref="RecipeId"/>.</summary>
    public RecipeEntity? Recipe { get; set; }

    public string Title { get; set; } = string.Empty;

    public int Portions { get; set; }

    public bool IsOptional { get; set; }

    public Guid? LeftoverOfMealId { get; set; }

    public MealStatus Status { get; set; }

    public List<PlannedMealIngredientEntity> CustomIngredients { get; set; } = [];
}
