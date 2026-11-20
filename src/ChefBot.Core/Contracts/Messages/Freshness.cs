using System.Text.Json.Serialization;

namespace ChefBot.Core.Contracts.Messages;

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
