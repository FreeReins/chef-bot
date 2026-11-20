namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Replace a meal with a recipe or with a free-form meal. Send exactly one of the two.</summary>
public sealed record ReplaceMealRequest(Guid? RecipeId = null, CustomMealRequest? Custom = null);
