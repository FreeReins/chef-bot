using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A recipe stored in ChefBot's own database.</summary>
internal sealed class Recipe
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    public decimal Servings { get; set; } = 4;

    public RecipeCourse Course { get; set; }

    public bool IsSlowCooker { get; set; }

    /// <summary>Archived recipes stay on past plans but are never planned again.</summary>
    public bool IsArchived { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>A short summary of the dish.</summary>
    public string? Description { get; set; }

    /// <summary>The steps, in order.</summary>
    public List<string> Instructions { get; set; } = [];

    public int? PrepMinutes { get; set; }

    public int? CookMinutes { get; set; }

    /// <summary>The whole time from start to table, which can be more than prep and cook added up.</summary>
    public int? TotalMinutes { get; set; }

    /// <summary>The web page the recipe came from.</summary>
    public string? SourceUrl { get; set; }

    public string? Author { get; set; }

    /// <summary>What the recipe makes, as the source words it: "one 9-inch pie".</summary>
    public string? YieldText { get; set; }

    /// <summary>The cook's own notes.</summary>
    public string? Notes { get; set; }

    public List<string> Tags { get; set; } = [];

    public List<RecipeIngredient> Ingredients { get; set; } = [];

    /// <summary>Photos, the cover first. Their bytes are kept apart, in <see cref="RecipeImageFile"/>.</summary>
    public List<RecipeImage> Images { get; set; } = [];
}
