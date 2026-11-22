namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One page of the recipe library, narrowed by words, course, tags, favourites and time.</summary>
/// <param name="Query">Words that must each appear in a recipe's name, ingredients or tags, in any case. Empty matches every recipe.</param>
/// <param name="Course">Only recipes of this course; null for all.</param>
/// <param name="Page">The page wanted, from 1.</param>
/// <param name="PageSize">How many recipes on a page, from 1 to 100.</param>
/// <param name="Tags">Only recipes with every one of these tags, in any case. Null or empty for any tags.</param>
/// <param name="Favourites">True for only recipes that are at least one person's favourite.</param>
/// <param name="FavouredBy">Only this person's favourites; null for anyone's (with <paramref name="Favourites"/>) or no limit.</param>
/// <param name="SlowCooker">True for only slow cooker recipes.</param>
/// <param name="ReadyWithin">Only recipes that say they are ready within this many minutes; null for any time.</param>
/// <param name="WithSuggestions">True for only recipes with AI suggestions waiting to be looked at.</param>
public sealed record RecipeSearchRequest(
    string? Query,
    RecipeCourse? Course,
    int Page,
    int PageSize,
    IReadOnlyList<string>? Tags = null,
    bool Favourites = false,
    Guid? FavouredBy = null,
    bool SlowCooker = false,
    int? ReadyWithin = null,
    bool WithSuggestions = false);
