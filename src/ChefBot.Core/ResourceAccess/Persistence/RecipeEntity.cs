using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class RecipeEntity
{
    public Guid Id { get; set; }

    public RecipeOrigin Origin { get; set; }

    public string? ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Servings { get; set; }

    public RecipeCourse Course { get; set; }

    public bool IsSlowCooker { get; set; }

    public bool IsArchived { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<RecipeIngredientEntity> Ingredients { get; set; } = [];
}
