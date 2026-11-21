namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe as a row in the library list: enough to recognise it, without its steps or photos.</summary>
/// <param name="Id">The recipe's id.</param>
/// <param name="Name">The recipe's name.</param>
/// <param name="Course">The kind of dish it is.</param>
/// <param name="IsSlowCooker">True when it is made in a slow cooker.</param>
/// <param name="Ingredients">Its ingredient lines as written.</param>
/// <param name="Tags">Its tags.</param>
public sealed record RecipeSummaryResponse(
    Guid Id,
    string Name,
    RecipeCourse Course,
    bool IsSlowCooker,
    IReadOnlyList<string> Ingredients,
    IReadOnlyList<string> Tags);
