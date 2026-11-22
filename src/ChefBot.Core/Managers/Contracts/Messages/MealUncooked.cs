using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a meal marked as cooked was taken back.</summary>
/// <param name="PlanId">The plan.</param>
/// <param name="MealId">The meal.</param>
[MessageIdentity("chefbot.meal-uncooked.v1")]
public sealed record MealUncooked(Guid PlanId, Guid MealId) : IKitchenMoment;
