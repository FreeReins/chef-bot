namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The sample recipes to add, by name.</summary>
public sealed record AddSamplesRequest(IReadOnlyList<string> Names);
