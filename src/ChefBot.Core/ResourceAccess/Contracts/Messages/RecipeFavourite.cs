namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>One person marking a recipe as one of their favourites. Each person has a recipe at most once.</summary>
internal sealed class RecipeFavourite
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RecipeId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>The person's display name as it is now. Filled in when read; never saved.</summary>
    public string UserName { get; set; } = string.Empty;
}
