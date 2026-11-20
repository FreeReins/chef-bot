using System.Text.Json.Serialization;

namespace ChefBot.Core.Contracts.Messages;

// Requests, responses, commands and events of IRecipeManager: the recipe library and importing into it.

/// <summary>What part of the week's eating a recipe is for.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeCourse>))]
public enum RecipeCourse
{
    /// <summary>A dinner, which may also feed the next days' lunches.</summary>
    Main,
    Breakfast,
    Snack,
    Sweet,

    /// <summary>Sides, drinks, sauces and anything else that is never planned on its own.</summary>
    Other,
}

/// <summary>Where a recipe came from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeOrigin>))]
public enum RecipeOrigin
{
    /// <summary>Entered or edited in ChefBot. Imports never overwrite it.</summary>
    Local,

    /// <summary>Imported from Mealie and refreshed by the next import.</summary>
    Mealie,
}

/// <summary>A recipe as someone types it in. Ingredients are free-text lines such as "2 cups kale".</summary>
public sealed record RecipeDraft(
    string Name,
    RecipeCourse Course,
    IReadOnlyList<string> Ingredients,
    bool IsSlowCooker = false,
    decimal? Servings = null);

/// <summary>A stored recipe.</summary>
public sealed record RecipeResponse(
    Guid Id,
    string Name,
    RecipeCourse Course,
    RecipeOrigin Origin,
    bool IsSlowCooker,
    decimal Servings,
    IReadOnlyList<string> Ingredients);

/// <summary>A recipe file to import: its name, which decides the format, and its text as read by the browser.</summary>
public sealed record ImportFileRequest(string FileName, string Content);

/// <summary>What importing a recipe file did.</summary>
/// <param name="Added">Recipes new to the library.</param>
/// <param name="AlreadyInLibrary">Recipes left alone because one with the same name is already there.</param>
/// <param name="Skipped">Entries without a name or any ingredients.</param>
public sealed record FileImportResult(int Added, int AlreadyInLibrary, int Skipped);

/// <summary>A kind of recipe file that can be imported, for the file picker.</summary>
public sealed record RecipeFileFormatResponse(string Name, IReadOnlyList<string> Extensions);

/// <summary>A sample recipe offered by the setup wizard.</summary>
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

/// <summary>The sample recipes to add, by name.</summary>
public sealed record AddSamplesRequest(IReadOnlyList<string> Names);

public sealed record AddSamplesResponse(int Added);

/// <summary>How to reach Mealie.</summary>
/// <param name="Url">Mealie's address, for example http://192.168.1.20:9000. Empty stops using Mealie.</param>
/// <param name="ApiToken">A new API token. Empty or null keeps the saved one.</param>
public sealed record MealieSettingsRequest(string? Url, string? ApiToken = null);

/// <summary>How ChefBot reaches Mealie. The token itself is never included.</summary>
/// <param name="Url">Mealie's address. Empty when Mealie isn't used.</param>
/// <param name="HasApiToken">True when an API token is saved.</param>
/// <param name="IsConfigured">True when importing from Mealie is possible.</param>
public sealed record MealieSettingsResponse(string Url, bool HasApiToken, bool IsConfigured);

/// <summary>What a Mealie import changed.</summary>
/// <param name="Added">Recipes seen for the first time.</param>
/// <param name="Updated">Imported recipes refreshed from the source.</param>
/// <param name="KeptLocal">Recipes left alone because they were edited or archived in ChefBot.</param>
/// <param name="Archived">Imported recipes that no longer exist in the source.</param>
/// <param name="Skipped">Recipes the source couldn't provide this time.</param>
public sealed record ImportResult(int Added, int Updated, int KeptLocal, int Archived, int Skipped);

// ---------- commands and events (sent through the message bus) ----------

/// <summary>Command: copy recipes from Mealie into the local database. Runs in the background.</summary>
/// <param name="StageWeekAfter">Set by the weekly schedule: the week to stage once the import is over.</param>
public sealed record ImportMealieRecipes(DateOnly? StageWeekAfter = null);

/// <summary>Event: a Mealie import finished, successfully or not.</summary>
/// <param name="Succeeded">False when Mealie isn't set up or couldn't be read.</param>
/// <param name="Result">What changed, when it succeeded.</param>
/// <param name="Problem">What went wrong, when it failed.</param>
/// <param name="StageWeekAfter">Carried over from <see cref="ImportMealieRecipes"/>.</param>
public sealed record RecipeImportFinished(bool Succeeded, ImportResult? Result, string? Problem, DateOnly? StageWeekAfter);
