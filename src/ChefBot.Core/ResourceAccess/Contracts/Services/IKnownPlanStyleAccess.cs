using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>The starter plan styles offered when choosing how to plan a week.</summary>
internal interface IKnownPlanStyleAccess
{
    IReadOnlyList<PlanStyle> All();
}
