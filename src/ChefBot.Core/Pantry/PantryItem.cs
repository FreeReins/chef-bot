using System.Text.Json.Serialization;
using ChefBot.Core.Ingredients;

namespace ChefBot.Core.Pantry;

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

/// <summary>Something in the kitchen: what it is, how much, when it was bought and when it expires.</summary>
public sealed class PantryItem : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>How much is left, or null when the amount isn't tracked.</summary>
    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateOnly PurchasedOn { get; set; }

    /// <summary>The expiration date, estimated from the catalog's shelf life unless one was entered.</summary>
    public DateOnly? ExpiresOn { get; set; }

    public bool IsExpired(DateOnly today) => ExpiresOn is DateOnly date && date < today;

    /// <summary>Days until it expires. Negative once it has, null when no date is known.</summary>
    public int? DaysLeft(DateOnly today) => ExpiresOn is DateOnly date ? date.DayNumber - today.DayNumber : null;

    public Freshness FreshnessOn(DateOnly today, int expiringSoonDays) => DaysLeft(today) switch
    {
        null => Freshness.Unknown,
        < 0 => Freshness.Expired,
        var days when days <= expiringSoonDays => Freshness.ExpiringSoon,
        _ => Freshness.Fresh,
    };
}
