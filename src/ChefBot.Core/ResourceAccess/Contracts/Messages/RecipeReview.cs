namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>One person's rating of a recipe, with any notes. A person can review a recipe as often as they cook it.</summary>
internal sealed class RecipeReview
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid RecipeId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>The reviewer's name when they wrote it, so the review still reads right after they leave.</summary>
    public string ReviewerName { get; set; } = string.Empty;

    /// <summary>From 1 to 5.</summary>
    public int Stars { get; set; }

    public string? Notes { get; set; }

    /// <summary>The household's day it was written, as the food history counts days.</summary>
    public DateOnly ReviewedOn { get; set; }

    /// <summary>When it was written, in UTC.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When it was last changed, or null when it never was.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
