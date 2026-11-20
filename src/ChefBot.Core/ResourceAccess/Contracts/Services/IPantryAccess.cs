using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>What is in the kitchen right now.</summary>
internal interface IPantryAccess
{
    Task<List<PantryItem>> ListAsync(CancellationToken cancellationToken);

    Task<PantryItem?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task SaveAsync(PantryItem item, CancellationToken cancellationToken);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
}
