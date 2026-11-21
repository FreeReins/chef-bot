using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>The starter styles: just the planner's own Classic week unless a test adds more.</summary>
internal sealed class FakeKnownPlanStyles : IKnownPlanStyleAccess
{
    public List<PlanStyle> Rows { get; } = [PlanStyle.Classic()];

    public IReadOnlyList<PlanStyle> All() => Rows;
}
