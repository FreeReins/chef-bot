using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>The items put on the shopping list by hand, apart from any plan.</summary>
internal interface IShoppingListAccess
{
    Task<List<ManualShoppingItem>> ListAsync(CancellationToken cancellationToken);

    Task<ManualShoppingItem?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task SaveAsync(ManualShoppingItem item, CancellationToken cancellationToken);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
}
