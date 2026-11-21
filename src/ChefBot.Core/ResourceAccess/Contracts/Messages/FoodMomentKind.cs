using System.Text.Json.Serialization;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>What happened in a stored <see cref="FoodMoment"/>. Written by name in backups.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FoodMomentKind>))]
internal enum FoodMomentKind
{
    /// <summary>A recipe joined the library, typed in or imported.</summary>
    RecipeAdded,

    /// <summary>A recipe was archived.</summary>
    RecipeRetired,

    /// <summary>A planned meal, not leftovers, was marked cooked.</summary>
    Cooked,

    /// <summary>Everything left on the shopping list was bought at once.</summary>
    Haul,

    /// <summary>The first account was made, when ChefBot was set up.</summary>
    KitchenOpened,

    /// <summary>Someone joined the household from an invite.</summary>
    PersonJoined,
}
