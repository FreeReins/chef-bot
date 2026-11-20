using System.Text.Json.Serialization;

namespace ChefBot.Core.Contracts.Messages;

// Requests, responses and events of IPantryManager: what is in the kitchen.

/// <summary>How close a pantry item is to its expiration date.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Freshness>))]
public enum Freshness
{
    /// <summary>No expiration date is known.</summary>
    Unknown,
    Fresh,
    ExpiringSoon,
    Expired,
}

/// <summary>The role an ingredient plays. Creative meals combine a whole grain, a protein and produce.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<IngredientGroup>))]
public enum IngredientGroup
{
    Other,
    WholeGrain,
    Protein,
    Vegetable,
    Fruit,
    Herb,
    Dairy,
    Staple,
}

/// <summary>Something added to the pantry by hand.</summary>
public sealed record NewPantryItem(
    string Name,
    decimal? Quantity = null,
    string? Unit = null,
    DateOnly? PurchasedOn = null,
    DateOnly? ExpiresOn = null);

/// <summary>Correct a pantry item. Null leaves a value unchanged.</summary>
public sealed record UpdatePantryItemRequest(decimal? Quantity = null, DateOnly? ExpiresOn = null);

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

/// <summary>Event: the pantry was edited by hand, so the open plans' shopping lists may be out of date.</summary>
public sealed record PantryChanged;
