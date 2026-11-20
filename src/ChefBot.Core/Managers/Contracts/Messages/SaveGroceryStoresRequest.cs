namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Replaces the household's stores. An empty list means shopping lists are grouped by food group instead.</summary>
public sealed record SaveGroceryStoresRequest(IReadOnlyList<GroceryStoreDetails>? Stores);
