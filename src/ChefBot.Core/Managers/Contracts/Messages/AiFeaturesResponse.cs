namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Which AI features are ready to use, because a model is set up for them.</summary>
/// <param name="RecipeFromPhoto">True when recipes can be read from photos.</param>
/// <param name="RecipeDetails">True when ChefBot can suggest what a recipe is missing.</param>
public sealed record AiFeaturesResponse(bool RecipeFromPhoto, bool RecipeDetails);
