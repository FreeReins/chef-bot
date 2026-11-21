using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class MealPlanEntity
{
    public Guid Id { get; set; }

    public DateOnly WeekStart { get; set; }

    public PlanStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string StyleName { get; set; } = string.Empty;

    public bool LeftoverLunches { get; set; } = true;

    public List<PlannedMealEntity> Meals { get; set; } = [];

    public List<ShoppingItemEntity> ShoppingItems { get; set; } = [];
}
