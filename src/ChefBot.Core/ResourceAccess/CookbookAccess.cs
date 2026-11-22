using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="ICookbookAccess"/>
internal sealed class CookbookAccess(ChefBotDbContext db) : ICookbookAccess
{
    public async Task<List<Cookbook>> ListAsync(CancellationToken cancellationToken)
    {
        // Loading them tracks them, so one added, renamed or removed earlier in this request is seen.
        await db.Cookbooks.LoadAsync(cancellationToken);
        return db.Cookbooks.Local
            .OrderBy(cookbook => cookbook.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(cookbook => cookbook.Id)
            .Select(cookbook => cookbook.ToContract())
            .ToList();
    }

    public async Task<Cookbook?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(id, cancellationToken))?.ToContract();

    public async Task SaveAsync(Cookbook cookbook, CancellationToken cancellationToken)
    {
        var fresh = cookbook.ToEntity();
        var tracked = await TrackedAsync(cookbook.Id, cancellationToken);
        if (tracked is null)
        {
            db.Cookbooks.Add(fresh);
        }
        else
        {
            db.CopyInto(tracked, fresh);
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await TrackedAsync(id, cancellationToken) is { } tracked)
        {
            db.Cookbooks.Remove(tracked);
        }
    }

    private async Task<CookbookEntity?> TrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.Cookbooks.Local.FirstOrDefault(cookbook => cookbook.Id == id)
        ?? await db.Cookbooks.FirstOrDefaultAsync(cookbook => cookbook.Id == id, cancellationToken);
}
