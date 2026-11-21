namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Replaces the household's plan styles. An empty list means planning uses the first starter style.</summary>
public sealed record SavePlanStylesRequest(IReadOnlyList<PlanStyleDetails>? Styles);
