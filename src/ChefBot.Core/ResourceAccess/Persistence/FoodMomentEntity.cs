using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class FoodMomentEntity
{
    public Guid Id { get; set; }

    public DateOnly OccurredOn { get; set; }

    public FoodMomentKind Kind { get; set; }

    public Guid? RecipeId { get; set; }

    public Guid? MealId { get; set; }

    public MealSlot? Slot { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? Count { get; set; }
}
