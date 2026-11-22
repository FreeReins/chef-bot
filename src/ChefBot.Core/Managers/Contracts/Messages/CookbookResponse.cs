namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A cookbook: a named group of recipes. A recipe can be in any number of cookbooks, or none.</summary>
/// <param name="Id">The cookbook's id.</param>
/// <param name="Name">The cookbook's name.</param>
/// <param name="Description">A line or two about what it is for, or null.</param>
/// <param name="Recipes">How many recipes that can be planned are in it.</param>
public sealed record CookbookResponse(Guid Id, string Name, string? Description, int Recipes);
