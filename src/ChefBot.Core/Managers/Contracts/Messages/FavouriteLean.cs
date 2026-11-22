using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// How much a plan style leans on the household's favourite recipes. Recipes planned recently are still left out
/// first, so favourites never come round more often than any other recipe would.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FavouriteLean>))]
public enum FavouriteLean
{
    /// <summary>Favourites count for nothing when choosing the week.</summary>
    Off,

    /// <summary>Favourites get a nudge, about as much as a recipe whose produce is in season.</summary>
    ALittle,

    /// <summary>Favourites are strongly preferred, ahead of most other reasons to pick a recipe.</summary>
    ALot,
}
