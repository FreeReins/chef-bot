using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A week of meals, snacks and sweets, with the shopping list that goes with it.</summary>
internal sealed class MealPlan
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The Sunday the week starts on. There is one plan per week.</summary>
    public DateOnly WeekStart { get; set; }

    public PlanStatus Status { get; set; } = PlanStatus.Staged;

    public DateTimeOffset CreatedAt { get; set; }

    public List<PlannedMeal> Meals { get; set; } = [];

    public List<ShoppingItem> ShoppingItems { get; set; } = [];
}
