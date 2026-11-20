namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Where to buy a shopping item from now on. The ingredient is added to that store's items and taken off any
/// other store's, so later lists send it there too. Empty or null stops sending it to a particular store.
/// </summary>
public sealed record ShoppingStoreRequest(string? Store);
