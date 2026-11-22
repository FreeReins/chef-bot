using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: recipes were added to the library, by hand, from a file, a web page or the samples.</summary>
/// <param name="RecipeIds">The recipes added.</param>
/// <param name="OccurredOn">The day they were added.</param>
/// <param name="OccurredAt">When, in UTC.</param>
[MessageIdentity("chefbot.recipes-added.v1")]
public sealed record RecipesAdded(IReadOnlyList<Guid> RecipeIds, DateOnly OccurredOn, DateTimeOffset OccurredAt) : IKitchenMoment;
