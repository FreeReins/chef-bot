using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IShoppingListAccess"/>
internal sealed class ShoppingListAccess(ChefBotDbContext db) : IShoppingListAccess
{
    public async Task<List<ManualShoppingItem>> ListAsync(CancellationToken cancellationToken) =>
        (await db.ManualShoppingItems.ToListAsync(cancellationToken)).Select(item => item.ToContract()).ToList();

    public async Task<ManualShoppingItem?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.ManualShoppingItems.FindAsync([id], cancellationToken))?.ToContract();

    public async Task SaveAsync(ManualShoppingItem item, CancellationToken cancellationToken)
    {
        var fresh = item.ToEntity();
        if (await db.ManualShoppingItems.FindAsync([item.Id], cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.ManualShoppingItems.Add(fresh);
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.ManualShoppingItems.FindAsync([id], cancellationToken) is { } item)
        {
            db.ManualShoppingItems.Remove(item);
        }
    }
}
