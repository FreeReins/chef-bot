namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How much of the library is missing details, for the page that offers to fill them in.</summary>
/// <param name="Sparse">Recipes missing details that have no suggestions for how they are now.</param>
/// <param name="Waiting">Recipes with suggestions waiting to be looked at.</param>
/// <param name="ModelName">The model that would make suggestions, or null when none is set up.</param>
public sealed record DetailsSweepStatusResponse(int Sparse, int Waiting, string? ModelName);
