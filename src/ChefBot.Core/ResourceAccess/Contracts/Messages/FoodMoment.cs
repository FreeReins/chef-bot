using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// One entry in the household's food history, written when it happens. The history page works out debuts,
/// milestones and the like from these when it is read, so only the plain facts are kept.
/// </summary>
internal sealed class FoodMoment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateOnly OccurredOn { get; set; }

    public FoodMomentKind Kind { get; set; }

    /// <summary>The recipe added, retired or cooked. Null for free-form meals and for moments about no recipe.</summary>
    public Guid? RecipeId { get; set; }

    /// <summary>The planned meal that was cooked, so undoing "cooked" can take the moment back.</summary>
    public Guid? MealId { get; set; }

    /// <summary>Which meal of the day was cooked.</summary>
    public MealSlot? Slot { get; set; }

    /// <summary>The recipe's, meal's or person's name as it was then.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>How many things were bought, for a haul.</summary>
    public int? Count { get; set; }
}
