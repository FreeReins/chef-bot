using ChefBot.Core.Utilities;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>An ingredient of a free-form meal. Amounts aren't tracked for these.</summary>
internal sealed class PlannedMealIngredient : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}
