using ChefBot.Core.Abstractions;
using ChefBot.Core.Common;
using ChefBot.Core.Planning;

namespace ChefBot.Core.Recipes;

/// <summary>What an import changed.</summary>
/// <param name="Added">Recipes seen for the first time.</param>
/// <param name="Updated">Imported recipes refreshed from the source.</param>
/// <param name="KeptLocal">Recipes left alone because they were edited or archived in ChefBot.</param>
/// <param name="Archived">Imported recipes that no longer exist in the source.</param>
/// <param name="Skipped">Recipes the source couldn't provide this time.</param>
public sealed record ImportResult(int Added, int Updated, int KeptLocal, int Archived, int Skipped);

/// <summary>Copies recipes from Mealie into ChefBot's database, so planning never depends on Mealie being up.</summary>
public sealed class RecipeImportService(
    IRecipeSource source,
    IRecipeRepository recipes,
    ISettingsStore settings,
    IUnitOfWork unitOfWork,
    RecipeClassifier classifier,
    RecipeIngredientBuilder ingredients,
    PlannerOptions options,
    TimeProvider clock)
{
    public Task<MealieSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
        settings.GetAsync<MealieSettings>(cancellationToken);

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken) =>
        (await GetSettingsAsync(cancellationToken)).IsConfigured;

    /// <summary>
    /// Saves how to reach Mealie. An empty address turns Mealie off and forgets the token. An empty token keeps
    /// the saved one, so the address can be changed without entering the token again.
    /// </summary>
    /// <exception cref="RuleViolationException">The address isn't a web address, or there is no token.</exception>
    public async Task<MealieSettings> UpdateSettingsAsync(string? url, string? apiToken, CancellationToken cancellationToken)
    {
        var current = await GetSettingsAsync(cancellationToken);
        var address = url?.Trim() ?? string.Empty;
        if (address.Length == 0)
        {
            current.Url = string.Empty;
            current.ApiToken = string.Empty;
        }
        else
        {
            if (!MealieSettings.IsWebAddress(address))
            {
                throw new RuleViolationException("Enter Mealie's full address, starting with http:// or https://.");
            }

            var token = string.IsNullOrWhiteSpace(apiToken) ? current.ApiToken : apiToken.Trim();
            if (token.Length == 0)
            {
                throw new RuleViolationException("Enter an API token. Create one in Mealie under your user profile, API Tokens.");
            }

            current.Url = address;
            current.ApiToken = token;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return current;
    }

    /// <summary>Uses the address and token from the configuration as the starting value, when none has been saved.</summary>
    public async Task UseDefaultConnectionAsync(string? url, string? apiToken, CancellationToken cancellationToken)
    {
        var current = await GetSettingsAsync(cancellationToken);
        if (current.Url.Length == 0 && !string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(apiToken))
        {
            current.Url = url.Trim();
            current.ApiToken = apiToken.Trim();
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <exception cref="ChefBot.Core.Common.RecipeSourceException">Mealie isn't configured or couldn't be read.</exception>
    public async Task<ImportResult> ImportAsync(CancellationToken cancellationToken)
    {
        var connection = await GetSettingsAsync(cancellationToken);
        if (!connection.IsConfigured)
        {
            throw new RecipeSourceException("Mealie isn't set up. Enter its address and an API token on the Settings tab.");
        }

        var fetched = await source.FetchAllAsync(connection, cancellationToken);
        var existing = (await recipes.ListAllAsync(cancellationToken))
            .Where(r => r.ExternalId is not null)
            .ToDictionary(r => r.ExternalId!, StringComparer.Ordinal);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        int added = 0, updated = 0, keptLocal = 0, archived = 0;

        foreach (var incoming in fetched.Recipes.Where(r => seen.Add(r.ExternalId)))
        {
            if (!existing.TryGetValue(incoming.ExternalId, out var recipe))
            {
                recipe = new Recipe { Origin = RecipeOrigin.Mealie, ExternalId = incoming.ExternalId };
                recipes.Add(recipe);
                added++;
            }
            else if (recipe.Origin == RecipeOrigin.Local)
            {
                keptLocal++;
                continue;
            }
            else
            {
                updated++;
            }

            Apply(incoming, recipe);
        }

        // Only a complete read may archive: a partial one must not hide recipes that are still there.
        if (fetched.Skipped == 0)
        {
            foreach (var missing in existing.Values.Where(r => r.Origin == RecipeOrigin.Mealie && !r.IsArchived && !seen.Contains(r.ExternalId!)))
            {
                missing.IsArchived = true;
                archived++;
            }
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return new ImportResult(added, updated, keptLocal, archived, fetched.Skipped);
    }

    private void Apply(SourceRecipe incoming, Recipe recipe)
    {
        var (course, isSlowCooker) = classifier.Classify(incoming.Name, incoming.Labels);
        recipe.Name = incoming.Name.Trim();
        recipe.Course = course;
        recipe.IsSlowCooker = isSlowCooker;
        recipe.Servings = incoming.Servings is > 0m ? incoming.Servings.Value : options.DefaultServings;
        recipe.IsArchived = false;
        recipe.UpdatedAt = clock.GetUtcNow();
        recipe.ReplaceIngredients(ingredients.FromSource(incoming.Ingredients));
    }
}
