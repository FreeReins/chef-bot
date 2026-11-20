using ChefBot.Core.Abstractions;
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
    IUnitOfWork unitOfWork,
    RecipeClassifier classifier,
    RecipeIngredientBuilder ingredients,
    PlannerOptions options,
    TimeProvider clock)
{
    public bool IsConfigured => source.IsConfigured;

    /// <exception cref="ChefBot.Core.Common.RecipeSourceException">Mealie isn't configured or couldn't be read.</exception>
    public async Task<ImportResult> ImportAsync(CancellationToken cancellationToken)
    {
        var fetched = await source.FetchAllAsync(cancellationToken);
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
