using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeCookbooks : ICookbookAccess
{
    public List<Cookbook> Rows { get; } = [];

    public Task<List<Cookbook>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Rows.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.Id).Select(Copies.Of).ToList());

    public Task<Cookbook?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(row => row.Id == id) is { } row ? Copies.Of(row) : null);

    public Task SaveAsync(Cookbook cookbook, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == cookbook.Id);
        Rows.Add(Copies.Of(cookbook));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == id);
        return Task.CompletedTask;
    }
}
