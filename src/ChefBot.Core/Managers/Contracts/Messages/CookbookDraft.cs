namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A cookbook as someone names it: a group of recipes such as "Weeknight" or "Grandma's".</summary>
/// <param name="Name">The cookbook's name, unique in any case.</param>
/// <param name="Description">A line or two about what the cookbook is for; null or empty for none.</param>
public sealed record CookbookDraft(string Name, string? Description = null);
