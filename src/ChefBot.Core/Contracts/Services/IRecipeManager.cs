using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Core.Contracts.Services;

/// <summary>The recipe library in ChefBot's own database, and importing into it from files, samples and Mealie.</summary>
public interface IRecipeManager
{
    /// <summary>Recipes that can be planned, by name.</summary>
    Task<IReadOnlyList<RecipeResponse>> ListAsync(CancellationToken cancellationToken);

    Task<RecipeResponse> CreateAsync(RecipeDraft draft, CancellationToken cancellationToken);

    /// <summary>Saves an edit. An imported recipe becomes a local one, so later imports leave the edit alone.</summary>
    Task<RecipeResponse> UpdateAsync(Guid id, RecipeDraft draft, CancellationToken cancellationToken);

    /// <summary>Stops a recipe from being planned. Past plans keep showing it.</summary>
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The kinds of recipe file that can be imported.</summary>
    IReadOnlyList<RecipeFileFormatResponse> GetFileFormats();

    /// <summary>Adds the recipes in an uploaded file. Importing the same file again adds nothing new.</summary>
    Task<FileImportResult> ImportFileAsync(ImportFileRequest request, CancellationToken cancellationToken);

    /// <summary>The setup wizard's sample recipes, marked when already in the library.</summary>
    Task<IReadOnlyList<SampleRecipeResponse>> GetSamplesAsync(CancellationToken cancellationToken);

    /// <summary>Adds the chosen samples. Names already in the library are skipped.</summary>
    Task<AddSamplesResponse> AddSamplesAsync(AddSamplesRequest request, CancellationToken cancellationToken);

    Task<MealieSettingsResponse> GetMealieSettingsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves how to reach Mealie. An empty address turns Mealie off and forgets the token. An empty token keeps
    /// the saved one, so the address can be changed without entering the token again.
    /// </summary>
    Task<MealieSettingsResponse> UpdateMealieSettingsAsync(MealieSettingsRequest request, CancellationToken cancellationToken);

    /// <summary>Uses the address and token from the configuration as the starting value, when none has been saved.</summary>
    Task UseDefaultMealieConnectionAsync(string? url, string? apiToken, CancellationToken cancellationToken);

    /// <summary>Queues a background Mealie import by sending <see cref="ImportMealieRecipes"/>.</summary>
    /// <exception cref="RuleViolationException">Mealie isn't set up.</exception>
    Task RequestMealieImportAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Copies recipes from Mealie, so planning never depends on Mealie being up. Publishes
    /// <see cref="RecipeImportFinished"/> whether it worked or not, and returns the same event.
    /// </summary>
    Task<RecipeImportFinished> ImportFromMealieAsync(ImportMealieRecipes command, CancellationToken cancellationToken);
}
