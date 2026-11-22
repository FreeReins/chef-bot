using ChefBot.Api.Live;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>
/// Tells the open pages which part of the kitchen each event changed, so they load it again. It only nudges: the
/// managers already saved the change, and the pages read it through the API.
/// </summary>
[StickyHandler(MessageQueues.Live)]
public static class LiveTopicsHandler
{
    public static void Handle(PlanChanged message, LiveUpdates live) => live.Broadcast(LiveTopics.Plan);

    public static void Handle(MealAdded message, LiveUpdates live) => live.Broadcast(LiveTopics.Plan);

    public static void Handle(PlanStaged message, LiveUpdates live) => live.Broadcast(LiveTopics.Plan);

    public static void Handle(PlanConfirmed message, LiveUpdates live) => live.Broadcast(LiveTopics.Plan);

    public static void Handle(PlanCleared message, LiveUpdates live) => live.Broadcast(LiveTopics.Plan);

    public static void Handle(MealCooked message, LiveUpdates live) => live.Broadcast(LiveTopics.Pantry);

    public static void Handle(MealUncooked message, LiveUpdates live) => live.Broadcast(LiveTopics.Pantry);

    public static void Handle(ShoppingItemBought message, LiveUpdates live) => live.Broadcast(LiveTopics.Pantry);

    public static void Handle(ShoppingDone message, LiveUpdates live) =>
        Broadcast(live, LiveTopics.Plan, LiveTopics.Shopping, LiveTopics.Pantry);

    public static void Handle(ManualShoppingChanged message, LiveUpdates live) => Broadcast(live, LiveTopics.Shopping, LiveTopics.Pantry);

    public static void Handle(PantryChanged message, LiveUpdates live) => live.Broadcast(LiveTopics.Pantry);

    public static void Handle(RecipesAdded message, LiveUpdates live) => live.Broadcast(LiveTopics.Recipes);

    public static void Handle(RecipeChanged message, LiveUpdates live) => live.Broadcast(LiveTopics.Recipes);

    public static void Handle(RecipeSuggestionsChanged message, LiveUpdates live) => live.Broadcast(LiveTopics.Recipes);

    public static void Handle(RecipeArchived message, LiveUpdates live) => live.Broadcast(LiveTopics.Recipes);

    public static void Handle(RecipeFavourited message, LiveUpdates live) => live.Broadcast(LiveTopics.Recipes);

    public static void Handle(RecipeUnfavourited message, LiveUpdates live) => live.Broadcast(LiveTopics.Recipes);

    public static void Handle(RecipeReviewed message, LiveUpdates live) => Broadcast(live, LiveTopics.Recipes, LiveTopics.History);

    public static void Handle(RecipeReviewRemoved message, LiveUpdates live) => Broadcast(live, LiveTopics.Recipes, LiveTopics.History);

    public static void Handle(HistoryRecorded message, LiveUpdates live) => live.Broadcast(LiveTopics.History);

    public static void Handle(AchievementsUnlocked message, LiveUpdates live) => live.Broadcast(LiveTopics.Achievements);

    private static void Broadcast(LiveUpdates live, params string[] topics)
    {
        foreach (var topic in topics)
        {
            live.Broadcast(topic);
        }
    }
}
