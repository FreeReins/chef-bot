using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Looking at and correcting what is in the kitchen. Open plans' shopping lists follow each change.</summary>
public static class PantryEndpoints
{
    /// <summary>Everything in the pantry, soonest to expire first.</summary>
    [WolverineGet("/api/pantry")]
    public static Task<IReadOnlyList<PantryItemResponse>> List([FromServices] IPantryManager pantry, CancellationToken cancellationToken) =>
        pantry.ListAsync(cancellationToken);

    /// <summary>Adds something by hand, for example food bought outside a plan.</summary>
    [WolverinePost("/api/pantry")]
    public static Task<PantryItemResponse> Add(NewPantryItem request, [FromServices] IPantryManager pantry, CancellationToken cancellationToken) =>
        pantry.AddAsync(request, cancellationToken);

    /// <summary>Corrects the amount left or the expiration date.</summary>
    [WolverinePatch("/api/pantry/{id}")]
    public static Task<PantryItemResponse> Update(
        UpdatePantryItemRequest request, Guid id, [FromServices] IPantryManager pantry, CancellationToken cancellationToken) =>
        pantry.UpdateAsync(id, request, cancellationToken);

    [WolverineDelete("/api/pantry/{id}")]
    public static Task Remove(Guid id, [FromServices] IPantryManager pantry, CancellationToken cancellationToken) =>
        pantry.RemoveAsync(id, cancellationToken);
}
