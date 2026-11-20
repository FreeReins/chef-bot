namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>An ingredient line split into its parts. Quantity is null when the line has no leading amount.</summary>
internal sealed record ParsedIngredient(decimal? Quantity, string Unit, string Name);
