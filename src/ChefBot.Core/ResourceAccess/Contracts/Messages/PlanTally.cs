namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>How many weekly plans are stored, and how many of them were confirmed.</summary>
/// <param name="Total">Every stored plan, drafts included.</param>
/// <param name="Confirmed">The plans that were confirmed.</param>
internal sealed record PlanTally(int Total, int Confirmed);
