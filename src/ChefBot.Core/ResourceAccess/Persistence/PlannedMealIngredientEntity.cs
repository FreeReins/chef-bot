namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class PlannedMealIngredientEntity
{
    public Guid Id { get; set; }

    public Guid PlannedMealId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}
