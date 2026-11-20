using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IPantryAccess"/>
internal sealed class PantryAccess(ChefBotDbContext db) : IPantryAccess
{
    public async Task<List<PantryItem>> ListAsync(CancellationToken cancellationToken) =>
        (await db.PantryItems.ToListAsync(cancellationToken)).Select(item => item.ToContract()).ToList();

    public async Task<PantryItem?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.PantryItems.FindAsync([id], cancellationToken))?.ToContract();

    public async Task SaveAsync(PantryItem item, CancellationToken cancellationToken)
    {
        var fresh = item.ToEntity();
        if (await db.PantryItems.FindAsync([item.Id], cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.PantryItems.Add(fresh);
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.PantryItems.FindAsync([id], cancellationToken) is { } item)
        {
            db.PantryItems.Remove(item);
        }
    }
}
