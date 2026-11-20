using System.Text.Json.Serialization;
using ChefBot.Api.Https;
using ChefBot.Core.Backups;
using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;
using ChefBot.Core.Shopping;
using ChefBot.Core.Swaps;

namespace ChefBot.Api.Contracts;

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

public sealed record ShoppingItemResponse(
    Guid Id,
    string Name,
    decimal? Quantity,
    string Unit,
    string Store,
    bool IsOptional,
    bool IsPurchased,
    DateOnly? PurchasedOn);

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

public sealed record RecipeResponse(
    Guid Id,
    string Name,
    RecipeCourse Course,
    RecipeOrigin Origin,
    bool IsSlowCooker,
    decimal Servings,
    IReadOnlyList<string> Ingredients);

/// <summary>Ways to replace one meal: recipes ranked by pantry use, and creative ideas.</summary>
public sealed record AlternativesResponse(
    MealResponse Meal,
    IReadOnlyList<RecipeAlternative> Recipes,
    IReadOnlyList<CreativeMeal> Creative);

/// <summary>The ingredients view: what the plan needs, and what is in the pantry.</summary>
public sealed record IngredientsResponse(IReadOnlyList<IngredientLine> Ingredients, IReadOnlyList<PantryUseResponse> Pantry);

/// <param name="Item">The pantry item.</param>
/// <param name="UsedByPlan">False when nothing on the plan uses it, which makes it a candidate for a swap.</param>
public sealed record PantryUseResponse(PantryItemResponse Item, bool UsedByPlan);

/// <summary>What the page needs to decide which screen to show.</summary>
/// <param name="SetupRequired">True until the first account exists.</param>
/// <param name="User">The signed-in account, or null.</param>
/// <param name="MealieConfigured">True when importing from Mealie is possible.</param>
public sealed record SessionResponse(bool SetupRequired, UserResponse? User, bool MealieConfigured);

/// <param name="Id">The account's id.</param>
/// <param name="Username">The name used to sign in.</param>
/// <param name="DisplayName">The name shown in the app.</param>
/// <param name="IsAdmin">True when the account can manage people and settings.</param>
/// <param name="CreatedAt">When the account was created.</param>
/// <param name="LockedUntil">Set while the account is locked after too many wrong passwords.</param>
public sealed record UserResponse(
    Guid Id, string Username, string DisplayName, bool IsAdmin, DateTimeOffset CreatedAt, DateTimeOffset? LockedUntil);

public sealed record SecurityResponse(int MaxFailedSignIns, int LockoutMinutes, bool RequireHttps);

/// <summary>How ChefBot is serving HTTPS right now.</summary>
/// <param name="Mode">Off, SelfSigned or Certificate.</param>
/// <param name="Enabled">True when an HTTPS address exists.</param>
/// <param name="Port">The HTTPS port to use in the browser.</param>
/// <param name="Hostnames">The names a self-signed certificate covers.</param>
/// <param name="ExpiresOn">When the certificate expires. Self-signed ones renew themselves at startup.</param>
/// <param name="CanRegenerate">True when ChefBot owns the certificate and can create a new one.</param>
/// <param name="IsSecureNow">True when this request arrived over HTTPS.</param>
/// <param name="RequireHttps">True when plain HTTP visitors are sent to HTTPS.</param>
public sealed record HttpsStatusResponse(
    HttpsMode Mode,
    bool Enabled,
    int Port,
    IReadOnlyList<string> Hostnames,
    DateTimeOffset? ExpiresOn,
    bool CanRegenerate,
    bool IsSecureNow,
    bool RequireHttps);

/// <param name="Name">The sample recipe's name.</param>
/// <param name="Course">The kind of dish it is.</param>
/// <param name="IsSlowCooker">True when it is made in a slow cooker.</param>
/// <param name="Servings">How many servings it makes, when known.</param>
/// <param name="Ingredients">Its ingredient lines.</param>
/// <param name="AlreadyAdded">True when a recipe with this name is already in the library.</param>
public sealed record SampleRecipeResponse(
    string Name,
    RecipeCourse Course,
    bool IsSlowCooker,
    decimal? Servings,
    IReadOnlyList<string> Ingredients,
    bool AlreadyAdded);

public sealed record AddSamplesResponse(int Added);

/// <summary>Backup settings and the backups that exist, newest first.</summary>
public sealed record BackupsResponse(
    bool Enabled,
    string Time,
    int KeepCount,
    int KeepDays,
    string LastResult,
    IReadOnlyList<BackupFile> Files);
