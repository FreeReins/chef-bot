using System.Security.Claims;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>
/// Reviews of recipes: stars from 1 to 5 and any notes. Each one gives back the recipe's reviews, newest first, and
/// the average rating they add up to.
/// </summary>
public static class ReviewEndpoints
{
    /// <summary>A recipe's reviews, newest first, with its rating.</summary>
    [WolverineGet("/api/recipes/{id}/reviews")]
    public static Task<RecipeReviewsResponse> List(Guid id, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.ListReviewsAsync(id, cancellationToken);

    /// <summary>Adds the signed-in person's review, such as <c>{ "stars": 4, "notes": "Less salt next time." }</c>.</summary>
    [WolverinePost("/api/recipes/{id}/reviews")]
    public static Task<RecipeReviewsResponse> Add(
        RecipeReviewRequest request, Guid id, ClaimsPrincipal principal, [FromServices] IRecipeManager recipes,
        CancellationToken cancellationToken) =>
        recipes.AddReviewAsync(id, Sessions.RequireUserId(principal), request, cancellationToken);

    /// <summary>Changes the stars and notes of one of the signed-in person's own reviews.</summary>
    [WolverinePut("/api/recipes/{id}/reviews/{reviewId}")]
    public static Task<RecipeReviewsResponse> Update(
        RecipeReviewRequest request, Guid id, Guid reviewId, ClaimsPrincipal principal, [FromServices] IRecipeManager recipes,
        CancellationToken cancellationToken) =>
        recipes.UpdateReviewAsync(id, reviewId, Sessions.RequireUserId(principal), request, cancellationToken);

    /// <summary>Deletes one of the signed-in person's own reviews. An admin can delete anyone's.</summary>
    [WolverineDelete("/api/recipes/{id}/reviews/{reviewId}")]
    public static Task<RecipeReviewsResponse> Remove(
        Guid id, Guid reviewId, ClaimsPrincipal principal, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RemoveReviewAsync(
            id, reviewId, Sessions.RequireUserId(principal), principal.IsInRole(Sessions.AdminRole), cancellationToken);
}
