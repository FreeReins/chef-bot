using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>A pantry item, and whether the plan is going to use it.</summary>
internal sealed record PantryLine(PantryItem Item, bool UsedByPlan);
