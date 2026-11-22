namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class RecipeReviewEntity
{
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }

    public Guid UserId { get; set; }

    public string ReviewerName { get; set; } = string.Empty;

    public int Stars { get; set; }

    public string? Notes { get; set; }

    public DateOnly ReviewedOn { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
