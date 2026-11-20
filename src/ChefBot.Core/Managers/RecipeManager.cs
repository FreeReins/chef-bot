using ChefBot.Core.Contracts;
using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Contracts.Services;
using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Managers;

/// <inheritdoc cref="IRecipeManager"/>
internal sealed class RecipeManager(
    IRecipeAccess recipes,
    ISettingsAccess settings,
    IRecipeSourceAccess mealie,
    ISampleRecipeAccess samples,
    IMessageAccess messages,
    TransformationEngine transformation,
    ValidationEngine validation,
    TimeProvider clock) : IRecipeManager
{
    private const string MealieNotSetUp = "Mealie isn't set up. Enter its address and an API token on the Settings tab.";

    public async Task<IReadOnlyList<RecipeResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await recipes.ListActiveAsync(cancellationToken)).Select(recipe => recipe.ToResponse()).ToList();

    public async Task<RecipeResponse> CreateAsync(RecipeDraft draft, CancellationToken cancellationToken)
    {
        var text = draft.ToText();
        ValidationEngine.Recipe(text);

        var recipe = new Recipe { Origin = RecipeOrigin.Local };
        transformation.ApplyText(text, recipe, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return recipe.ToResponse();
    }

    public async Task<RecipeResponse> UpdateAsync(Guid id, RecipeDraft draft, CancellationToken cancellationToken)
    {
        var recipe = await validation.RecipeAsync(id, cancellationToken);
        var text = draft.ToText();
        ValidationEngine.Recipe(text);

        recipe.Origin = RecipeOrigin.Local;
        transformation.ApplyText(text, recipe, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return recipe.ToResponse();
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await validation.RecipeAsync(id, cancellationToken);
        recipe.IsArchived = true;
        recipe.Origin = RecipeOrigin.Local;
        await recipes.SaveAsync(recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public IReadOnlyList<RecipeFileFormatResponse> GetFileFormats() =>
        transformation.Formats.Select(format => new RecipeFileFormatResponse(format.Name, format.Extensions)).ToList();

    public async Task<FileImportResult> ImportFileAsync(ImportFileRequest request, CancellationToken cancellationToken)
    {
        var file = transformation.ReadFile(request.FileName ?? string.Empty, request.Content ?? string.Empty);
        var added = await AddNewAsync(file.Recipes, cancellationToken);
        return new FileImportResult(added, file.Recipes.Count - added, file.Skipped);
    }

    public async Task<IReadOnlyList<SampleRecipeResponse>> GetSamplesAsync(CancellationToken cancellationToken)
    {
        var library = await recipes.ListActiveAsync(cancellationToken);
        var existing = library.Select(recipe => recipe.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return samples.All()
            .Select(sample => new SampleRecipeResponse(
                sample.Name, sample.Course, sample.IsSlowCooker, sample.Servings, sample.Ingredients, existing.Contains(sample.Name)))
            .ToList();
    }

    public async Task<AddSamplesResponse> AddSamplesAsync(AddSamplesRequest request, CancellationToken cancellationToken)
    {
        var wanted = (request.Names ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var chosen = samples.All().Where(sample => wanted.Contains(sample.Name)).ToList();
        return new AddSamplesResponse(await AddNewAsync(chosen, cancellationToken));
    }

    public async Task<MealieSettingsResponse> GetMealieSettingsAsync(CancellationToken cancellationToken) =>
        (await settings.GetMealieAsync(cancellationToken)).ToResponse();

    public async Task<MealieSettingsResponse> UpdateMealieSettingsAsync(MealieSettingsRequest request, CancellationToken cancellationToken)
    {
        var current = await settings.GetMealieAsync(cancellationToken);
        (current.Url, current.ApiToken) = ValidationEngine.MealieConnection(request.Url, request.ApiToken, current.ApiToken);
        await settings.SaveMealieAsync(current, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return current.ToResponse();
    }

    public async Task UseDefaultMealieConnectionAsync(string? url, string? apiToken, CancellationToken cancellationToken)
    {
        var current = await settings.GetMealieAsync(cancellationToken);
        if (current.Url.Length == 0 && !string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(apiToken))
        {
            current.Url = url.Trim();
            current.ApiToken = apiToken.Trim();
            await settings.SaveMealieAsync(current, cancellationToken);
        }

        await messages.CommitAsync(cancellationToken);
    }

    public async Task RequestMealieImportAsync(CancellationToken cancellationToken)
    {
        ValidationEngine.MealieIsSetUp(await settings.GetMealieAsync(cancellationToken));
        await messages.PublishAsync(new ImportMealieRecipes());
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<RecipeImportFinished> ImportFromMealieAsync(ImportMealieRecipes command, CancellationToken cancellationToken)
    {
        RecipeImportFinished finished;
        var connection = await settings.GetMealieAsync(cancellationToken);
        if (!connection.IsConfigured)
        {
            finished = new RecipeImportFinished(false, null, MealieNotSetUp, command.StageWeekAfter);
        }
        else
        {
            try
            {
                var fetched = await mealie.FetchAllAsync(connection, cancellationToken);
                var changes = transformation.MergeImport(fetched, await recipes.ListAllAsync(cancellationToken), clock.GetUtcNow());
                foreach (var recipe in changes.Changed)
                {
                    await recipes.SaveAsync(recipe, cancellationToken);
                }

                finished = new RecipeImportFinished(true, changes.Result, null, command.StageWeekAfter);
            }
            catch (RecipeSourceException exception)
            {
                // An unreachable Mealie is an expected outcome: planning carries on from the saved recipes.
                finished = new RecipeImportFinished(false, null, exception.Message, command.StageWeekAfter);
            }
        }

        await messages.PublishAsync(finished);
        await messages.CommitAsync(cancellationToken);
        return finished;
    }

    /// <summary>Adds the recipes whose names aren't in the library yet.</summary>
    /// <returns>How many were added.</returns>
    private async Task<int> AddNewAsync(IReadOnlyList<RecipeText> candidates, CancellationToken cancellationToken)
    {
        var fresh = transformation.NotYetInLibrary(candidates, await recipes.ListAllAsync(cancellationToken));
        foreach (var text in fresh)
        {
            ValidationEngine.Recipe(text);
            var recipe = new Recipe { Origin = RecipeOrigin.Local };
            transformation.ApplyText(text, recipe, clock.GetUtcNow());
            await recipes.SaveAsync(recipe, cancellationToken);
        }

        await messages.CommitAsync(cancellationToken);
        return fresh.Count;
    }
}
