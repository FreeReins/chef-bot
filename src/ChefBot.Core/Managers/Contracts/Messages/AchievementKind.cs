using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What an <see cref="Achievement"/> is for: a first step in the kitchen, or a badge with tiers.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AchievementKind>))]
public enum AchievementKind
{
    /// <summary>First step: the library holds a recipe.</summary>
    FirstRecipe,
    /// <summary>First step: a week was planned.</summary>
    FirstPlan,
    /// <summary>First step: a plan was confirmed.</summary>
    PlanConfirmed,
    /// <summary>First step: everything left on a shopping list was bought at once.</summary>
    FirstShop,
    /// <summary>First step: a planned meal was marked cooked.</summary>
    FirstCook,
    /// <summary>First step: a recipe was made someone's favourite.</summary>
    FirstFavourite,
    /// <summary>Optional first step, never scored: someone joined the household from an invite.</summary>
    FirstGuest,
    /// <summary>Badge: recipes in the library.</summary>
    Cookbook,
    /// <summary>Badge: dinners cooked.</summary>
    Dinners,
    /// <summary>Badge: different recipes cooked.</summary>
    Repertoire,
    /// <summary>Badge: weeks planned.</summary>
    WeeksPlanned,
    /// <summary>Badge: whole shopping lists bought at once.</summary>
    Hauls,
    /// <summary>Badge: the longest run ever of weeks in a row with something cooked.</summary>
    BestStreak,
    /// <summary>Badge: recipes cooked again after a long rest.</summary>
    Comebacks,
    /// <summary>Badge: reviews written.</summary>
    Critic,
    /// <summary>Badge: favourites marked.</summary>
    Favourites,
}
