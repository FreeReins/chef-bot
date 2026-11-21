namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One page of the recipe library, narrowed by words and course.</summary>
/// <param name="Query">Words that must each appear in a recipe's name, ingredients or tags, in any case. Empty matches every recipe.</param>
/// <param name="Course">Only recipes of this course; null for all.</param>
/// <param name="Page">The page wanted, from 1.</param>
/// <param name="PageSize">How many recipes on a page, from 1 to 100.</param>
public sealed record RecipeSearchRequest(string? Query, RecipeCourse? Course, int Page, int PageSize);
