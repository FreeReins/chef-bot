using ChefBot.Core.Abstractions;
using ChefBot.Core.Common;

namespace ChefBot.Core.Recipes;

/// <summary>A recipe as someone types it in. Ingredients are free-text lines such as "2 cups kale".</summary>
public sealed record RecipeDraft(
    string Name,
    RecipeCourse Course,
    IReadOnlyList<string> Ingredients,
    bool IsSlowCooker = false,
    decimal? Servings = null);

/// <summary>Manages the recipes kept in ChefBot's own database.</summary>
public sealed class RecipeService(
    IRecipeRepository recipes,
    IUnitOfWork unitOfWork,
    RecipeIngredientBuilder ingredients,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<Recipe>> ListAsync(CancellationToken cancellationToken) =>
        await recipes.ListActiveAsync(cancellationToken);

    public async Task<Recipe> CreateAsync(RecipeDraft draft, CancellationToken cancellationToken)
    {
        var recipe = new Recipe { Origin = RecipeOrigin.Local };
        Apply(draft, recipe);
        recipes.Add(recipe);
        await unitOfWork.CommitAsync(cancellationToken);
        return recipe;
    }

    /// <summary>Saves an edit. An imported recipe becomes a local one, so later imports leave the edit alone.</summary>
    public async Task<Recipe> UpdateAsync(Guid id, RecipeDraft draft, CancellationToken cancellationToken)
    {
        var recipe = await recipes.GetRequiredAsync(id, cancellationToken);
        recipe.Origin = RecipeOrigin.Local;
        Apply(draft, recipe);
        await unitOfWork.CommitAsync(cancellationToken);
        return recipe;
    }

    /// <summary>Stops a recipe from being planned. Past plans keep showing it.</summary>
    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await recipes.GetRequiredAsync(id, cancellationToken);
        recipe.IsArchived = true;
        recipe.Origin = RecipeOrigin.Local;
        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <summary>Adds the chosen recipes, skipping any whose name is already in the library.</summary>
    /// <returns>How many recipes were added.</returns>
    public async Task<int> AddManyAsync(IReadOnlyList<RecipeDraft> drafts, CancellationToken cancellationToken)
    {
        var existing = await recipes.ListAllAsync(cancellationToken);
        var taken = existing.Select(recipe => recipe.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var draft in drafts.Where(d => taken.Add(d.Name?.Trim() ?? string.Empty)))
        {
            var recipe = new Recipe { Origin = RecipeOrigin.Local };
            Apply(draft, recipe);
            recipes.Add(recipe);
            added++;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return added;
    }

    private void Apply(RecipeDraft draft, Recipe recipe)
    {
        if (string.IsNullOrWhiteSpace(draft.Name))
        {
            throw new RuleViolationException("A recipe needs a name.");
        }

        if (draft.Servings is <= 0m)
        {
            throw new RuleViolationException("Servings must be more than zero.");
        }

        var lines = ingredients.FromLines(draft.Ingredients ?? []);
        if (lines.Count == 0)
        {
            throw new RuleViolationException("A recipe needs at least one ingredient.");
        }

        recipe.Name = draft.Name.Trim();
        recipe.Course = draft.Course;
        recipe.IsSlowCooker = draft.IsSlowCooker;
        recipe.Servings = draft.Servings ?? recipe.Servings;
        recipe.UpdatedAt = clock.GetUtcNow();
        recipe.ReplaceIngredients(lines);
    }
}
