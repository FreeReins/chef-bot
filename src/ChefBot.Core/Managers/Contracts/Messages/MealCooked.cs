using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a planned meal was marked as cooked.</summary>
/// <param name="PlanId">The plan.</param>
/// <param name="MealId">The meal.</param>
/// <param name="RecipeId">Its recipe, if it has one.</param>
/// <param name="Slot">Breakfast, lunch or dinner.</param>
/// <param name="Title">The meal's name when it was cooked.</param>
/// <param name="IsLeftover">True for leftovers, which the history leaves out.</param>
/// <param name="OccurredOn">The day it counts as cooked: its own day when marked late, otherwise today.</param>
/// <param name="OccurredAt">When it was marked, in UTC; null when marked after its day.</param>
/// <param name="FavouredBy">Who had the recipe as a favourite then.</param>
[MessageIdentity("chefbot.meal-cooked.v1")]
public sealed record MealCooked(
    Guid PlanId,
    Guid MealId,
    Guid? RecipeId,
    MealSlot Slot,
    string Title,
    bool IsLeftover,
    DateOnly OccurredOn,
    DateTimeOffset? OccurredAt,
    IReadOnlyList<string> FavouredBy) : IKitchenMoment;
