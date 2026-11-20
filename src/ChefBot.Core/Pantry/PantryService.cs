using ChefBot.Core.Abstractions;
using ChefBot.Core.Common;
using ChefBot.Core.Ingredients;

namespace ChefBot.Core.Pantry;

/// <summary>Something added to the pantry by hand.</summary>
public sealed record NewPantryItem(
    string Name,
    decimal? Quantity = null,
    string? Unit = null,
    DateOnly? PurchasedOn = null,
    DateOnly? ExpiresOn = null);

/// <summary>Lets people look at and correct what ChefBot thinks is in the kitchen.</summary>
public sealed class PantryService(
    IPantryRepository pantry,
    IUnitOfWork unitOfWork,
    IngredientCatalog catalog,
    PantryItemFactory factory,
    TimeProvider clock)
{
    /// <summary>Everything in the pantry, soonest to expire first.</summary>
    public async Task<IReadOnlyList<PantryItem>> ListAsync(CancellationToken cancellationToken)
    {
        var items = await pantry.ListAsync(cancellationToken);
        return items
            .OrderBy(item => item.ExpiresOn ?? DateOnly.MaxValue)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<PantryItem> AddAsync(NewPantryItem request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RuleViolationException("A pantry item needs a name.");
        }

        var item = factory.Create(
            catalog.Identify(request.Name),
            request.Quantity,
            request.Unit,
            request.PurchasedOn ?? clock.Today(),
            request.ExpiresOn);

        pantry.Add(item);
        await unitOfWork.CommitAsync(cancellationToken);
        return item;
    }

    /// <summary>Corrects the amount left or the expiration date. Null leaves a value as it is.</summary>
    public async Task<PantryItem> UpdateAsync(Guid id, decimal? quantity, DateOnly? expiresOn, CancellationToken cancellationToken)
    {
        var item = await pantry.GetRequiredAsync(id, cancellationToken);
        if (quantity is < 0m)
        {
            throw new RuleViolationException("A quantity can't be negative.");
        }

        item.Quantity = quantity ?? item.Quantity;
        item.ExpiresOn = expiresOn ?? item.ExpiresOn;
        await unitOfWork.CommitAsync(cancellationToken);
        return item;
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        pantry.Remove(await pantry.GetRequiredAsync(id, cancellationToken));
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
