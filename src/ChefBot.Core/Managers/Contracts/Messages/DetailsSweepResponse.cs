namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How many recipes were queued for suggestions, and which model will make them.</summary>
/// <param name="Queued">Recipes queued. Suggestions appear on each as they are ready.</param>
/// <param name="ModelName">The model that will make them.</param>
public sealed record DetailsSweepResponse(int Queued, string ModelName);
