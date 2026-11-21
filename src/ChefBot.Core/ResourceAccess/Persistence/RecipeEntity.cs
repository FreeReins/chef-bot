using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class RecipeEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Servings { get; set; }

    public RecipeCourse Course { get; set; }

    public bool IsSlowCooker { get; set; }

    public bool IsArchived { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? Description { get; set; }

    public List<string> Instructions { get; set; } = [];

    public int? PrepMinutes { get; set; }

    public int? CookMinutes { get; set; }

    public int? TotalMinutes { get; set; }

    public string? SourceUrl { get; set; }

    public string? Author { get; set; }

    public string? YieldText { get; set; }

    public string? Notes { get; set; }

    public List<string> Tags { get; set; } = [];

    public List<RecipeIngredientEntity> Ingredients { get; set; } = [];

    public List<RecipeImageEntity> Images { get; set; } = [];
}
