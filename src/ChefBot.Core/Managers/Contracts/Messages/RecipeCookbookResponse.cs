namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A cookbook named on a recipe, a plan style or a report.</summary>
/// <param name="Id">The cookbook's id.</param>
/// <param name="Name">The cookbook's name.</param>
public sealed record RecipeCookbookResponse(Guid Id, string Name);
