using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers.Contracts.Services;

/// <summary>
/// Plans weeks and keeps each plan, its leftover lunches and its shopping list in step. Every change to a plan
/// goes through here: staging, confirming, swapping, moving or cooking meals, recording purchases, and choosing the
/// stores the shopping list is split between and the plan styles weeks are planned in. It also keeps the items put on the shopping list by hand, which need no plan.
/// </summary>
public interface IPlanManager
{
    /// <summary>
    /// Proposes a plan for a week in a plan style and saves it as staged. Meals listed in <see cref="StagePlanRequest.Keep"/>
    /// are carried over from the week's current plan. Publishes <see cref="PlanStaged"/>.
    /// </summary>
    Task<PlanResponse> StageAsync(StagePlanRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Rebuilds a week around what is in the pantry, typically after shopping: recipes that use food already
    /// bought count for much more, and the new shopping list only asks for what is still missing.
    /// </summary>
    Task<PlanResponse> RegenerateFromPantryAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Removes a plan. The pantry is left as it is, so anything already bought stays on hand.</summary>
    Task ClearAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>The plan for this week, or else the next upcoming one.</summary>
    Task<PlanResponse> GetCurrentAsync(CancellationToken cancellationToken);

    Task<PlanResponse> GetAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Every plan whose week hasn't ended, earliest first: this week's and any staged for later weeks.</summary>
    Task<IReadOnlyList<PlanSummaryResponse>> ListOpenAsync(CancellationToken cancellationToken);

    /// <summary>Makes a staged plan the working plan, which opens it up for shopping.</summary>
    Task<PlanResponse> ConfirmAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Recalculates leftovers and the shopping list, for example after the pantry was edited by hand.</summary>
    Task<PlanResponse> RefreshAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Refreshes every plan whose week hasn't ended. Run after <see cref="PantryChanged"/>.</summary>
    Task RefreshOpenPlansAsync(CancellationToken cancellationToken);

    /// <summary>What the plan needs, where each ingredient comes from, and what the pantry holds.</summary>
    Task<IngredientsResponse> GetIngredientsAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Suggests replacements for a meal: recipes that use the pantry, and creative ideas for lunch and dinner.</summary>
    Task<AlternativesResponse> GetAlternativesAsync(Guid planId, Guid mealId, int recipeLimit, int creativeCount, CancellationToken cancellationToken);

    /// <summary>Replaces a meal with a recipe or a free-form meal.</summary>
    Task<PlanResponse> ReplaceMealAsync(Guid planId, Guid mealId, ReplaceMealRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a dinner or breakfast to another day of its week, swapping it with a meal of the same kind already there.
    /// Leftover lunches and the shopping list follow.
    /// </summary>
    Task<PlanResponse> MoveMealAsync(Guid planId, Guid mealId, MoveMealRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a meal as planned, cooked or skipped. Cooking a meal takes its ingredients out of the pantry where
    /// amounts can be compared. Undoing "cooked" does not put them back.
    /// </summary>
    Task<PlanResponse> SetMealStatusAsync(Guid planId, Guid mealId, MealStatusRequest request, CancellationToken cancellationToken);

    /// <summary>Ticks one shopping item off and puts it in the pantry with an expiration date.</summary>
    Task<PlanResponse> PurchaseAsync(Guid planId, Guid itemId, PurchaseDetails details, CancellationToken cancellationToken);

    /// <summary>
    /// Marks everything still on the plan's list as bought on the same day, together with everything added to the
    /// shopping list by hand.
    /// </summary>
    Task<PlanResponse> PurchaseAllAsync(Guid planId, PurchaseAllRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a shopping item's ingredient to another store from now on, and rebuilds the shopping lists of every plan
    /// whose week hasn't ended to match.
    /// </summary>
    Task<PlanResponse> SetShoppingStoreAsync(Guid planId, Guid itemId, ShoppingStoreRequest request, CancellationToken cancellationToken);

    /// <summary>What was put on the shopping list by hand, each with the store it is bought at.</summary>
    Task<IReadOnlyList<ManualShoppingItemResponse>> ListManualItemsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Puts something on the shopping list by hand, with or without a plan. Adding the same thing in the same unit
    /// again adds to the amount already listed. Returns the whole hand-added list.
    /// </summary>
    Task<IReadOnlyList<ManualShoppingItemResponse>> AddManualItemAsync(NewShoppingItem request, CancellationToken cancellationToken);

    /// <summary>Takes a hand-added item off the shopping list without buying it. Returns the whole hand-added list.</summary>
    Task<IReadOnlyList<ManualShoppingItemResponse>> RemoveManualItemAsync(Guid itemId, CancellationToken cancellationToken);

    /// <summary>
    /// Buys a hand-added item: it leaves the list and goes into the pantry, and open plans' shopping lists drop what it
    /// covers. Works whether or not a plan is confirmed. Returns the whole hand-added list.
    /// </summary>
    Task<IReadOnlyList<ManualShoppingItemResponse>> PurchaseManualItemAsync(Guid itemId, PurchaseDetails details, CancellationToken cancellationToken);

    /// <summary>Buys every hand-added item on the same day. Returns the hand-added list, which is then empty.</summary>
    Task<IReadOnlyList<ManualShoppingItemResponse>> PurchaseAllManualItemsAsync(PurchaseAllRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Buys a hand-added item's ingredient at another store from now on, like <see cref="SetShoppingStoreAsync"/>.
    /// Returns the whole hand-added list.
    /// </summary>
    Task<IReadOnlyList<ManualShoppingItemResponse>> SetManualItemStoreAsync(
        Guid itemId, ShoppingStoreRequest request, CancellationToken cancellationToken);

    /// <summary>The meal plan's calendar feed, with its address under <paramref name="site"/>, ChefBot's own address.</summary>
    Task<CalendarFeedResponse> GetCalendarFeedAsync(Uri site, CancellationToken cancellationToken);

    /// <summary>Turns the calendar feed on with a new address. An address made before stops working.</summary>
    Task<CalendarFeedResponse> StartCalendarFeedAsync(Uri site, CancellationToken cancellationToken);

    /// <summary>Turns the calendar feed off. Its address stops working.</summary>
    Task<CalendarFeedResponse> StopCalendarFeedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The calendar feed's text (iCalendar): the meals of the confirmed plans from last week on, linking to their
    /// recipes under <paramref name="site"/>.
    /// </summary>
    /// <exception cref="NotFoundException">The feed is off, or <paramref name="token"/> isn't its secret.</exception>
    Task<string> GetCalendarAsync(string token, Uri site, CancellationToken cancellationToken);

    /// <summary>The to-do apps the shopping list can be sent to, in their saved order. Tokens are never included.</summary>
    Task<IReadOnlyList<ShoppingTargetResponse>> GetShoppingTargetsAsync(CancellationToken cancellationToken);

    /// <summary>Replaces the to-do apps the shopping list can be sent to. A target sent without a token keeps its saved one.</summary>
    /// <exception cref="RuleViolationException">A target is missing its address, list or token, or two share a name.</exception>
    Task<IReadOnlyList<ShoppingTargetResponse>> SaveShoppingTargetsAsync(SaveShoppingTargetsRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Adds what is left to buy to a to-do app, one item per thing to buy with its amount, store and meals. Nothing
    /// is ticked off in ChefBot. Sending again adds the items again.
    /// </summary>
    /// <exception cref="NotFoundException">The to-do app or the plan doesn't exist.</exception>
    /// <exception cref="RuleViolationException">Nothing is left to buy.</exception>
    Task<SendShoppingListResult> SendShoppingListAsync(SendShoppingListRequest request, CancellationToken cancellationToken);

    /// <summary>Adds one test item to a to-do app, so its settings can be checked.</summary>
    /// <exception cref="NotFoundException">The to-do app doesn't exist.</exception>
    Task<SendShoppingListResult> SendTestShoppingItemAsync(Guid targetId, CancellationToken cancellationToken);

    /// <summary>The household's stores, the known stores that can be added, and the ingredient names a store can list.</summary>
    Task<GroceryStoresResponse> GetStoresAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the household's stores and rebuilds the shopping lists of every plan whose week hasn't ended.
    /// With no stores, shopping lists are grouped by food group.
    /// </summary>
    Task<GroceryStoresResponse> SaveStoresAsync(SaveGroceryStoresRequest request, CancellationToken cancellationToken);

    /// <summary>The household's plan styles, the starter styles that can be added, and the tags and ingredients rules can use.</summary>
    Task<PlanStylesResponse> GetStylesAsync(CancellationToken cancellationToken);

    /// <summary>Replaces the household's plan styles. Plans already made keep the style they were made with.</summary>
    Task<PlanStylesResponse> SaveStylesAsync(SavePlanStylesRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Stages next week's plan when the weekly schedule's time has come, once per week, unless that week already
    /// has a plan.
    /// </summary>
    Task CheckWeeklyScheduleAsync(CancellationToken cancellationToken);

    /// <summary>When the weekly schedule next stages a plan, or null when it is switched off.</summary>
    Task<DateTimeOffset?> NextScheduledStagingAsync(CancellationToken cancellationToken);

    /// <summary>How many people plans are for, when next week is planned by itself, and how long leftovers keep.</summary>
    Task<PlannerSettingsResponse> GetPlannerSettingsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves how many people plans are for, the weekly schedule and how long leftovers keep. Weeks not yet confirmed
    /// are resized for the new number of people, with their shopping lists; confirmed weeks keep theirs.
    /// </summary>
    Task<PlannerSettingsResponse> SavePlannerSettingsAsync(PlannerSettingsRequest request, CancellationToken cancellationToken);

    /// <summary>When meals appear in the calendar feed.</summary>
    Task<CalendarTimesResponse> GetCalendarTimesAsync(CancellationToken cancellationToken);

    /// <summary>Saves when meals appear in the calendar feed. Subscribed calendars pick it up when they next refresh.</summary>
    Task<CalendarTimesResponse> SaveCalendarTimesAsync(CalendarTimesRequest request, CancellationToken cancellationToken);
}
