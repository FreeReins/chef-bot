using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Contracts.Services;
using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <inheritdoc cref="IPantryManager"/>
internal sealed class PantryManager(
    IPantryAccess pantry,
    IMessageAccess messages,
    TransformationEngine transformation,
    ValidationEngine validation,
    IngredientCatalog catalog,
    PlannerOptions options,
    TimeProvider clock) : IPantryManager
{
    public async Task<IReadOnlyList<PantryItemResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var today = clock.Today();
        var items = await pantry.ListAsync(cancellationToken);
        return items
            .OrderBy(item => item.ExpiresOn ?? DateOnly.MaxValue)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.ToResponse(today, options.ExpiringSoonDays, catalog))
            .ToList();
    }

    public async Task<PantryItemResponse> AddAsync(NewPantryItem request, CancellationToken cancellationToken)
    {
        ValidationEngine.PantryItemName(request.Name);
        var purchasedOn = request.PurchasedOn ?? clock.Today();
        ValidationEngine.Stock(request.Quantity, purchasedOn, request.ExpiresOn);

        var item = transformation.ToPantryItem(request.Name, request.Quantity, request.Unit, purchasedOn, request.ExpiresOn);
        await pantry.SaveAsync(item, cancellationToken);
        await messages.PublishAsync(new PantryChanged());
        await messages.CommitAsync(cancellationToken);
        return item.ToResponse(clock.Today(), options.ExpiringSoonDays, catalog);
    }

    public async Task<PantryItemResponse> UpdateAsync(Guid id, UpdatePantryItemRequest request, CancellationToken cancellationToken)
    {
        var item = await validation.PantryItemAsync(id, cancellationToken);
        ValidationEngine.QuantityLeft(request.Quantity);

        item.Quantity = request.Quantity ?? item.Quantity;
        item.ExpiresOn = request.ExpiresOn ?? item.ExpiresOn;
        await pantry.SaveAsync(item, cancellationToken);
        await messages.PublishAsync(new PantryChanged());
        await messages.CommitAsync(cancellationToken);
        return item.ToResponse(clock.Today(), options.ExpiringSoonDays, catalog);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await validation.PantryItemAsync(id, cancellationToken);
        await pantry.RemoveAsync(item.Id, cancellationToken);
        await messages.PublishAsync(new PantryChanged());
        await messages.CommitAsync(cancellationToken);
    }
}
