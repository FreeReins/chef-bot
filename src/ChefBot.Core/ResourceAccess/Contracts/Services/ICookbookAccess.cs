using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>Stored cookbooks. Which recipes are in each is kept on the recipes (<see cref="Recipe.CookbookIds"/>).</summary>
internal interface ICookbookAccess
{
    /// <summary>Every cookbook, by name.</summary>
    Task<List<Cookbook>> ListAsync(CancellationToken cancellationToken);

    Task<Cookbook?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Adds the cookbook, or updates the stored one with the same id.</summary>
    Task SaveAsync(Cookbook cookbook, CancellationToken cancellationToken);

    /// <summary>Removes the cookbook. Doing it twice changes nothing.</summary>
    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
}
