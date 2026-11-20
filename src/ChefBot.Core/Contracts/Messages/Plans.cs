using System.Text.Json.Serialization;

namespace ChefBot.Core.Contracts.Messages;

// Requests, responses, commands and events of IPlanManager: weekly plans, their meals and their shopping lists.

/// <summary>Whether a plan is still a proposal or the one being shopped and cooked from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PlanStatus>))]
public enum PlanStatus
{
    /// <summary>A proposal to review, shuffle and adjust. Nothing has been bought against it.</summary>
    Staged,

    /// <summary>The plan the household is shopping and cooking from.</summary>
    Confirmed,
}

/// <summary>Which part of the day or week a planned meal fills.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MealSlot>))]
public enum MealSlot
{
    Breakfast,
    Lunch,
    Dinner,
    Snack,
    Sweet,
}

/// <summary>Whether a planned meal is still to come, was cooked, or was skipped.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MealStatus>))]
public enum MealStatus
{
    Planned,
    Cooked,
    Skipped,
}

/// <summary>Where an ingredient the plan needs will come from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<IngredientStatus>))]
public enum IngredientStatus
{
    ToBuy,
    Purchased,
    InPantry,
    Staple,
}

// ---------- requests ----------

/// <summary>Stage a plan. Send <c>{}</c> for the upcoming week.</summary>
/// <param name="WeekStart">The Sunday to plan from. Defaults to the upcoming week.</param>
/// <param name="Replace">Start a week over even though it was already confirmed.</param>
public sealed record StagePlanRequest(DateOnly? WeekStart = null, bool Replace = false);

/// <summary>Replace a meal with a recipe or with a free-form meal. Send exactly one of the two.</summary>
public sealed record ReplaceMealRequest(Guid? RecipeId = null, CustomMealRequest? Custom = null);

/// <summary>A free-form meal, for example one of the creative suggestions.</summary>
public sealed record CustomMealRequest(string Title, IReadOnlyList<string> Ingredients);

/// <summary>Mark a meal as planned, cooked or skipped.</summary>
public sealed record MealStatusRequest(MealStatus Status);

/// <summary>Details recorded when something is bought. Anything left null is filled in.</summary>
/// <param name="PurchasedOn">Defaults to today.</param>
/// <param name="ExpiresOn">The date on the package. Defaults to an estimate from the ingredient's shelf life.</param>
/// <param name="Quantity">What was actually bought, when it differs from the list.</param>
public sealed record PurchaseDetails(DateOnly? PurchasedOn = null, DateOnly? ExpiresOn = null, decimal? Quantity = null);

/// <summary>Mark every open shopping item as bought. Send <c>{}</c> for today.</summary>
public sealed record PurchaseAllRequest(DateOnly? PurchasedOn = null);

// ---------- responses ----------

/// <summary>A weekly plan with its meals and shopping list.</summary>
/// <param name="Id">The plan's id.</param>
/// <param name="WeekStart">The first day of the planned week.</param>
/// <param name="WeekEnd">The last day of the planned week.</param>
/// <param name="Status">Staged while it is a proposal to review; Confirmed once the household shops and cooks from it.</param>
/// <param name="Meals">The planned meals, snacks and sweets.</param>
/// <param name="Shopping">What to buy for the plan, and where.</param>
/// <param name="Notes">Things worth knowing about the change that produced this response.</param>
public sealed record PlanResponse(
    Guid Id,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    PlanStatus Status,
    IReadOnlyList<MealResponse> Meals,
    IReadOnlyList<ShoppingItemResponse> Shopping,
    IReadOnlyList<string> Notes);

/// <summary>One entry on a plan.</summary>
/// <param name="Id">The meal's id.</param>
/// <param name="Date">The suggested day. Null for snacks and sweets.</param>
/// <param name="Day">The weekday name of <paramref name="Date"/>.</param>
/// <param name="Slot">Which part of the day or week the meal fills.</param>
/// <param name="Title">The name shown for the meal.</param>
/// <param name="RecipeId">The recipe it follows. Null for a free-form meal.</param>
/// <param name="Status">Whether the meal is planned, cooked or skipped.</param>
/// <param name="IsOptional">True when the meal is planned but its groceries are marked as skippable.</param>
/// <param name="IsLeftover">True when the meal is eating up leftovers of another meal.</param>
/// <param name="IsSlowCooker">True when the meal is made in a slow cooker.</param>
/// <param name="Portions">Portions to cook. Zero means "make the recipe once, as written".</param>
/// <param name="Ingredients">The names of the ingredients the meal uses.</param>
public sealed record MealResponse(
    Guid Id,
    DateOnly? Date,
    string? Day,
    MealSlot Slot,
    string Title,
    Guid? RecipeId,
    MealStatus Status,
    bool IsOptional,
    bool IsLeftover,
    bool IsSlowCooker,
    int Portions,
    IReadOnlyList<string> Ingredients);

/// <summary>One line of a plan's shopping list.</summary>
public sealed record ShoppingItemResponse(
    Guid Id,
    string Name,
    decimal? Quantity,
    string Unit,
    string Store,
    bool IsOptional,
    bool IsPurchased,
    DateOnly? PurchasedOn);

/// <summary>Ways to replace one meal: recipes ranked by pantry use, and creative ideas.</summary>
public sealed record AlternativesResponse(
    MealResponse Meal,
    IReadOnlyList<RecipeAlternative> Recipes,
    IReadOnlyList<CreativeMeal> Creative);

/// <summary>A recipe that could replace a planned meal, and what choosing it would mean.</summary>
/// <param name="RecipeId">The recipe to swap in.</param>
/// <param name="Name">Its name.</param>
/// <param name="IsSlowCooker">True for slow cooker recipes.</param>
/// <param name="Score">Higher is a better fit for what is on hand and in season.</param>
/// <param name="OnHand">Ingredients already in the pantry.</param>
/// <param name="ExpiringSoon">Pantry ingredients it would use before they expire.</param>
/// <param name="ToBuy">Ingredients it would add to the shopping list.</param>
public sealed record RecipeAlternative(
    Guid RecipeId,
    string Name,
    bool IsSlowCooker,
    double Score,
    IReadOnlyList<string> OnHand,
    IReadOnlyList<string> ExpiringSoon,
    IReadOnlyList<string> ToBuy);

/// <summary>One part of a creative meal.</summary>
/// <param name="Name">The ingredient.</param>
/// <param name="Group">Its role: whole grain, protein, vegetable or fruit.</param>
/// <param name="OnHand">False when it would have to be bought.</param>
/// <param name="ExpiresOn">When the pantry's stock of it expires, if known.</param>
public sealed record CreativeComponent(string Name, IngredientGroup Group, bool OnHand, DateOnly? ExpiresOn);

/// <summary>A meal idea built from a whole grain, a protein and a vegetable or fruit.</summary>
public sealed record CreativeMeal(string Title, IReadOnlyList<CreativeComponent> Components)
{
    /// <summary>The ingredient names, ready to send back when choosing this idea.</summary>
    public IReadOnlyList<string> Ingredients => Components.Select(component => component.Name).ToList();
}

/// <summary>The ingredients view: what the plan needs, and what is in the pantry.</summary>
public sealed record IngredientsResponse(IReadOnlyList<IngredientLine> Ingredients, IReadOnlyList<PantryUseResponse> Pantry);

/// <summary>One ingredient the plan needs, and which meals need it.</summary>
public sealed record IngredientLine(
    string Name,
    IngredientStatus Status,
    decimal? Quantity,
    string Unit,
    string? Store,
    DateOnly? ExpiresOn,
    bool IsOptional,
    IReadOnlyList<string> UsedIn,
    Guid? ShoppingItemId);

/// <param name="Item">The pantry item.</param>
/// <param name="UsedByPlan">False when nothing on the plan uses it, which makes it a candidate for a swap.</param>
public sealed record PantryUseResponse(PantryItemResponse Item, bool UsedByPlan);

// ---------- commands and events (sent through the message bus) ----------

/// <summary>Event: a plan was staged and is ready for review.</summary>
/// <param name="PlanId">The staged plan.</param>
/// <param name="Trigger">What staged it: "schedule", "manual" or "regenerate".</param>
public sealed record PlanStaged(Guid PlanId, string Trigger);

/// <summary>Command, every few minutes: stage next week's plan if the weekly schedule's time has come.</summary>
public sealed record CheckWeeklySchedule;

/// <summary>Command: stage a week for the weekly schedule, once its Mealie import has finished or failed.</summary>
public sealed record StageScheduledWeek(DateOnly WeekStart);
