using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Engines;

/// <summary>A pantry item, and whether the plan is going to use it.</summary>
internal sealed record PantryLine(PantryItem Item, bool UsedByPlan);
