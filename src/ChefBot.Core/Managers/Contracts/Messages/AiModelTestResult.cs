namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Whether an AI model answered a short test question.</summary>
/// <param name="Worked">True when it answered.</param>
/// <param name="Problem">What went wrong, when it didn't.</param>
public sealed record AiModelTestResult(bool Worked, string? Problem = null);
