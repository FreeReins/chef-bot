using ChefBot.Core.Abstractions;
using ChefBot.Core.Common;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;

namespace ChefBot.Core.Shopping;

/// <summary>Details recorded when something is bought. Anything left null is filled in.</summary>
/// <param name="PurchasedOn">Defaults to today.</param>
/// <param name="ExpiresOn">The date on the package. Defaults to an estimate from the ingredient's shelf life.</param>
/// <param name="Quantity">What was actually bought, when it differs from the list.</param>
public sealed record PurchaseDetails(DateOnly? PurchasedOn = null, DateOnly? ExpiresOn = null, decimal? Quantity = null);

/// <summary>Records purchases: ticks items off the list and puts them in the pantry with an expiration date.</summary>
public sealed class ShoppingService(
    IMealPlanRepository plans,
    IPantryRepository pantry,
    IUnitOfWork unitOfWork,
    PantryItemFactory factory,
    TimeProvider clock)
{
    public async Task<PlanView> PurchaseAsync(Guid planId, Guid itemId, PurchaseDetails details, CancellationToken cancellationToken)
    {
        var plan = await GetConfirmedPlanAsync(planId, cancellationToken);
        var item = plan.GetShoppingItem(itemId);
        if (item.IsPurchased)
        {
            throw new ConflictException($"{item.DisplayName} is already marked as purchased.");
        }

        Stock(item, details);
        await unitOfWork.CommitAsync(cancellationToken);
        return new PlanView(plan, []);
    }

    /// <summary>Marks everything still on the list as bought on the same day.</summary>
    public async Task<PlanView> PurchaseAllAsync(Guid planId, DateOnly? purchasedOn, CancellationToken cancellationToken)
    {
        var plan = await GetConfirmedPlanAsync(planId, cancellationToken);
        foreach (var item in plan.ShoppingItems.Where(item => !item.IsPurchased))
        {
            Stock(item, new PurchaseDetails(purchasedOn));
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return new PlanView(plan, []);
    }

    private async Task<MealPlan> GetConfirmedPlanAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await plans.GetRequiredAsync(planId, cancellationToken);
        return plan.Status == PlanStatus.Confirmed
            ? plan
            : throw new ConflictException("Confirm the staged plan before recording purchases against it.");
    }

    private void Stock(ShoppingItem item, PurchaseDetails details)
    {
        var purchasedOn = details.PurchasedOn ?? clock.Today();
        pantry.Add(factory.Create(item, details.Quantity ?? item.Quantity, item.Unit, purchasedOn, details.ExpiresOn));
        item.IsPurchased = true;
        item.PurchasedOn = purchasedOn;
    }
}
