using System.Security.Claims;
using ChefBot.Api.Contracts;
using ChefBot.Core.Accounts;
using ChefBot.Core.Recipes;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>
/// How ChefBot reaches Mealie to import recipes. Admins only. Mealie__Url and Mealie__ApiToken give the
/// starting values until something is saved here. The token can be replaced but is never sent back.
/// </summary>
public static class MealieEndpoints
{
    [WolverineGet("/api/system/mealie")]
    public static async Task<MealieSettingsResponse> GetSettings(
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] RecipeImportService imports,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return (await imports.GetSettingsAsync(cancellationToken)).ToResponse();
    }

    /// <summary>
    /// Saves Mealie's address and API token. Leave the token empty to keep the saved one, or send an empty
    /// address to stop using Mealie.
    /// </summary>
    [WolverinePut("/api/system/mealie")]
    public static async Task<MealieSettingsResponse> UpdateSettings(
        MealieSettingsRequest request,
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] RecipeImportService imports,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return (await imports.UpdateSettingsAsync(request.Url, request.ApiToken, cancellationToken)).ToResponse();
    }
}
