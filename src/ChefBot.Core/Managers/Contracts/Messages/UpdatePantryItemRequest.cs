namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Correct a pantry item. Null leaves a value unchanged.</summary>
public sealed record UpdatePantryItemRequest(decimal? Quantity = null, DateOnly? ExpiresOn = null);
