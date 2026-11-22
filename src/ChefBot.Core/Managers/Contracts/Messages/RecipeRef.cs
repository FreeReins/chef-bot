namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe named in a report.</summary>
/// <param name="Id">The recipe's id.</param>
/// <param name="Name">The recipe's name.</param>
public sealed record RecipeRef(Guid Id, string Name);
