using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class MealPlanEntity
{
    public Guid Id { get; set; }

    public DateOnly WeekStart { get; set; }

    public PlanStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<PlannedMealEntity> Meals { get; set; } = [];

    public List<ShoppingItemEntity> ShoppingItems { get; set; } = [];
}
