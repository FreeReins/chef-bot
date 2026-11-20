using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>A recipe that could replace a planned meal, and what choosing it would mean.</summary>
internal sealed record Alternative(
    Recipe Recipe,
    double Score,
    IReadOnlyList<string> OnHand,
    IReadOnlyList<string> ExpiringSoon,
    IReadOnlyList<string> ToBuy);
