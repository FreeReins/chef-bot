namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One page of a recipe search, in course order and then by name.</summary>
/// <param name="Recipes">The recipes on this page.</param>
/// <param name="Total">How many recipes match the search across all pages.</param>
/// <param name="Page">The page shown, from 1.</param>
/// <param name="PageSize">How many recipes a full page holds.</param>
/// <param name="LibrarySize">How many recipes can be planned, matching or not.</param>
public sealed record RecipePageResponse(
    IReadOnlyList<RecipeSummaryResponse> Recipes,
    int Total,
    int Page,
    int PageSize,
    int LibrarySize);
