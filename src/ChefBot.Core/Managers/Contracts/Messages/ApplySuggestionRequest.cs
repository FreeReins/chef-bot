namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Which suggested changes to make to a recipe.</summary>
/// <param name="ChangeIds">The <see cref="SuggestedChange.Id"/> of each change to make. The rest are left out.</param>
public sealed record ApplySuggestionRequest(IReadOnlyList<int>? ChangeIds);
