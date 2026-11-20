using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A recipe stored in ChefBot's own database.</summary>
internal sealed class Recipe
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public RecipeOrigin Origin { get; set; }

    /// <summary>The id in the system it was imported from, if any.</summary>
    public string? ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Servings { get; set; } = 4;

    public RecipeCourse Course { get; set; }

    public bool IsSlowCooker { get; set; }

    /// <summary>Archived recipes stay on past plans but are never planned again.</summary>
    public bool IsArchived { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<RecipeIngredient> Ingredients { get; set; } = [];
}
