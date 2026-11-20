using System.Security.Claims;
using ChefBot.Api.Contracts;
using ChefBot.Core.Accounts;
using ChefBot.Core.Recipes;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Setup wizard, step two: reviewing the sample recipes and choosing which to add. Admins only.</summary>
public static class SetupEndpoints
{
    /// <summary>The sample recipes, with their ingredients, marked when already in the library.</summary>
    [WolverineGet("/api/setup/samples")]
    public static async Task<IReadOnlyList<SampleRecipeResponse>> Samples(
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] RecipeService recipes,
        [FromServices] SampleRecipes samples,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        var library = await recipes.ListAsync(cancellationToken);
        var existing = library.Select(recipe => recipe.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return samples.All
            .Select(sample => new SampleRecipeResponse(
                sample.Name, sample.Course, sample.IsSlowCooker, sample.Servings, sample.Ingredients, existing.Contains(sample.Name)))
            .ToList();
    }

    /// <summary>Adds the chosen samples to the library. Names already in the library are skipped.</summary>
    [WolverinePost("/api/setup/samples")]
    public static async Task<AddSamplesResponse> AddSamples(
        AddSamplesRequest request,
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] RecipeService recipes,
        [FromServices] SampleRecipes samples,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        var wanted = (request.Names ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var chosen = samples.All.Where(sample => wanted.Contains(sample.Name)).ToList();
        return new AddSamplesResponse(await recipes.AddManyAsync(chosen, cancellationToken));
    }
}
