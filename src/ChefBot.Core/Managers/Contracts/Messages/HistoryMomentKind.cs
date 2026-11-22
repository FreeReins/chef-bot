using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What a <see cref="HistoryMoment"/> on the food history timeline tells.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<HistoryMomentKind>))]
public enum HistoryMomentKind
{
    /// <summary>ChefBot was set up. <see cref="HistoryMoment.Title"/> is the first person's name.</summary>
    KitchenOpened,

    /// <summary>Someone joined the household. <see cref="HistoryMoment.Title"/> is their name.</summary>
    PersonJoined,

    /// <summary>A recipe joined the library.</summary>
    RecipeAdded,

    /// <summary>Many recipes joined on one day, such as an import. <see cref="HistoryMoment.Count"/> says how many; <see cref="HistoryMoment.Dishes"/> names a few.</summary>
    RecipeStampede,

    /// <summary>A recipe was archived.</summary>
    RecipeRetired,

    /// <summary>A recipe was cooked for the first time.</summary>
    Debut,

    /// <summary>A recipe reached a round number of cooks: 5, 10, 25, 50 or 100. <see cref="HistoryMoment.Count"/> is that number.</summary>
    FanFavourite,

    /// <summary>A recipe was cooked again after a long rest. <see cref="HistoryMoment.Count"/> is the days since it was last cooked.</summary>
    Comeback,

    /// <summary>
    /// An everyday cook of a recipe someone had as a favourite. <see cref="HistoryMoment.FavouredBy"/> names them.
    /// A debut, fan favourite or comeback of a favourite stays that kind and names them the same way.
    /// </summary>
    FavouriteCooked,

    /// <summary>The household's 50th, 100th, 250th, 500th or 1000th dinner. <see cref="HistoryMoment.Count"/> is the number; <see cref="HistoryMoment.Title"/> the dinner.</summary>
    DinnerMilestone,

    /// <summary>The everyday cooking of one week, in <see cref="HistoryMoment.Dishes"/>.</summary>
    WeekOfPlates,

    /// <summary>Everything left on the shopping list was bought at once. <see cref="HistoryMoment.Count"/> is how many things.</summary>
    Haul,
}
