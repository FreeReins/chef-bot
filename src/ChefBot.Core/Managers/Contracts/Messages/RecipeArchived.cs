using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a recipe was archived, so it is no longer planned.</summary>
/// <param name="RecipeId">The recipe.</param>
/// <param name="OccurredOn">The day it was archived.</param>
/// <param name="OccurredAt">When, in UTC.</param>
[MessageIdentity("chefbot.recipe-archived.v1")]
public sealed record RecipeArchived(Guid RecipeId, DateOnly OccurredOn, DateTimeOffset OccurredAt) : IKitchenMoment;
