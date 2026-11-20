using ChefBot.Api.Contracts;
using ChefBot.Core.Common;
using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Looking at and correcting what is in the kitchen.</summary>
public static class PantryEndpoints
{
    /// <summary>Everything in the pantry, soonest to expire first.</summary>
    [WolverineGet("/api/pantry")]
    public static async Task<IReadOnlyList<PantryItemResponse>> List(
        [FromServices] PantryService pantry,
        [FromServices] PlannerOptions options,
        [FromServices] IngredientCatalog catalog,
        [FromServices] TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var today = clock.Today();
        var items = await pantry.ListAsync(cancellationToken);
        return items.Select(item => item.ToResponse(today, options, catalog)).ToList();
    }

    /// <summary>Adds something by hand, for example food bought outside a plan.</summary>
    [WolverinePost("/api/pantry")]
    public static async Task<PantryItemResponse> Add(
        NewPantryItem request,
        [FromServices] PantryService pantry,
        [FromServices] PlannerOptions options,
        [FromServices] IngredientCatalog catalog,
        [FromServices] TimeProvider clock,
        CancellationToken cancellationToken) =>
        (await pantry.AddAsync(request, cancellationToken)).ToResponse(clock.Today(), options, catalog);

    /// <summary>Corrects the amount left or the expiration date.</summary>
    [WolverinePatch("/api/pantry/{id}")]
    public static async Task<PantryItemResponse> Update(
        UpdatePantryItemRequest request,
        Guid id,
        [FromServices] PantryService pantry,
        [FromServices] PlannerOptions options,
        [FromServices] IngredientCatalog catalog,
        [FromServices] TimeProvider clock,
        CancellationToken cancellationToken) =>
        (await pantry.UpdateAsync(id, request.Quantity, request.ExpiresOn, cancellationToken)).ToResponse(clock.Today(), options, catalog);

    [WolverineDelete("/api/pantry/{id}")]
    public static Task Remove(Guid id, [FromServices] PantryService pantry, CancellationToken cancellationToken) =>
        pantry.RemoveAsync(id, cancellationToken);
}
