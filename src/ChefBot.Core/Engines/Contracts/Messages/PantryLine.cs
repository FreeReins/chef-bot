using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>A pantry item, and the planned meals that are going to use it, soonest first.</summary>
internal sealed record PantryLine(PantryItem Item, IReadOnlyList<PlannedMeal> UsedFor)
{
    public bool UsedByPlan => UsedFor.Count > 0;
}
