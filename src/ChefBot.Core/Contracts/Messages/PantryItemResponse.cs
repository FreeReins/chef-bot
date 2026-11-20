namespace ChefBot.Core.Contracts.Messages;

/// <summary>Something in the kitchen.</summary>
/// <param name="Id">The pantry item's id.</param>
/// <param name="Name">The ingredient's name.</param>
/// <param name="Quantity">How much is left. Null when the amount isn't tracked.</param>
/// <param name="Unit">The unit <paramref name="Quantity"/> is measured in.</param>
/// <param name="PurchasedOn">The day it was bought.</param>
/// <param name="ExpiresOn">The day it expires. Null when it doesn't.</param>
/// <param name="DaysLeft">Days until it expires; negative once it has.</param>
/// <param name="Freshness">How close it is to its expiration date.</param>
/// <param name="Group">The ingredient's role, when the catalog knows it.</param>
public sealed record PantryItemResponse(
    Guid Id,
    string Name,
    decimal? Quantity,
    string Unit,
    DateOnly PurchasedOn,
    DateOnly? ExpiresOn,
    int? DaysLeft,
    Freshness Freshness,
    IngredientGroup? Group);
