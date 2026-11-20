using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Core.Contracts.Services;

/// <summary>Lets people look at and correct what ChefBot thinks is in the kitchen. Changes publish <see cref="PantryChanged"/>.</summary>
public interface IPantryManager
{
    /// <summary>Everything in the pantry, soonest to expire first.</summary>
    Task<IReadOnlyList<PantryItemResponse>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Adds something by hand, for example food bought outside a plan.</summary>
    Task<PantryItemResponse> AddAsync(NewPantryItem request, CancellationToken cancellationToken);

    /// <summary>Corrects the amount left or the expiration date.</summary>
    Task<PantryItemResponse> UpdateAsync(Guid id, UpdatePantryItemRequest request, CancellationToken cancellationToken);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
}
