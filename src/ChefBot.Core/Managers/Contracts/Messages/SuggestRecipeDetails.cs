using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Command: ask the AI model chosen for completing recipes what one recipe is missing, and keep its suggestions.
/// Sent by the library sweep; handled by calling <see cref="Services.IRecipeManager.SuggestDetailsForSweepAsync"/>.
/// </summary>
/// <param name="RecipeId">The recipe.</param>
[MessageIdentity("chefbot.suggest-recipe-details.v1")]
public sealed record SuggestRecipeDetails(Guid RecipeId);
