namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe read from photos, ready to check and save. Nothing is saved yet.</summary>
/// <param name="Draft">What was read, to fill in the recipe form.</param>
/// <param name="ReadWith">The name of the AI model that read it, such as "Claude".</param>
public sealed record RecipePhotoResponse(RecipeDraft Draft, string ReadWith);
