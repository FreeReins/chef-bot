using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <content>Checks on cookbooks and the recipes put in them.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>The longest name a cookbook can have.</summary>
    public const int MaxCookbookName = 80;

    /// <summary>The longest description a cookbook can have.</summary>
    public const int MaxCookbookDescription = 500;

    /// <summary>The most recipes put in a cookbook at once.</summary>
    private const int MaxCookbookRecipesAtOnce = 500;

    /// <summary>A cookbook that exists.</summary>
    /// <exception cref="NotFoundException">There is no such cookbook.</exception>
    public async Task<Cookbook> CookbookAsync(Guid id, CancellationToken cancellationToken) =>
        await cookbooks.FindAsync(id, cancellationToken)
        ?? throw new NotFoundException("That cookbook no longer exists. Reload the page and choose another.");

    /// <summary>
    /// Checks a cookbook's name and description, and returns them tidied: the name has words and no other cookbook
    /// has it in any case; an empty description becomes null.
    /// </summary>
    /// <param name="draft">The name and description given.</param>
    /// <param name="renaming">The cookbook being renamed, which may keep its own name; null for a new one.</param>
    /// <param name="cancellationToken">Stops the check.</param>
    public async Task<(string Name, string? Description)> CookbookDraftAsync(CookbookDraft? draft, Guid? renaming, CancellationToken cancellationToken)
    {
        var name = string.Join(' ', (draft?.Name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (name.Length == 0)
        {
            throw new RuleViolationException("Give the cookbook a name.");
        }

        if (name.Length > MaxCookbookName)
        {
            throw new RuleViolationException($"Keep cookbook names to {MaxCookbookName} characters or fewer.");
        }

        var description = draft?.Description?.Trim();
        if (description?.Length > MaxCookbookDescription)
        {
            throw new RuleViolationException($"Keep the cookbook's description to {MaxCookbookDescription} characters or fewer.");
        }

        var taken = (await cookbooks.ListAsync(cancellationToken))
            .FirstOrDefault(other => other.Id != renaming && string.Equals(other.Name, name, StringComparison.OrdinalIgnoreCase));
        if (taken is not null)
        {
            throw new RuleViolationException($"There's already a cookbook called {taken.Name}. Pick another name.");
        }

        return (name, string.IsNullOrEmpty(description) ? null : description);
    }

    /// <summary>The cookbooks a recipe is put in, each once.</summary>
    /// <exception cref="NotFoundException">One of them doesn't exist.</exception>
    public async Task<List<Guid>> CookbookIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var known = (await cookbooks.ListAsync(cancellationToken)).Select(cookbook => cookbook.Id).ToHashSet();
        var wanted = ids.Distinct().ToList();
        return wanted.All(known.Contains)
            ? wanted
            : throw new NotFoundException("One of those cookbooks no longer exists. Reload the page and choose again.");
    }

    /// <summary>The recipes to put in a cookbook: each one once, and each one that can be planned.</summary>
    public async Task<List<Recipe>> RecipesForCookbookAsync(IReadOnlyList<Guid>? ids, CancellationToken cancellationToken)
    {
        var wanted = (ids ?? []).Distinct().ToList();
        if (wanted.Count == 0)
        {
            throw new RuleViolationException("Choose at least one recipe to add to the cookbook.");
        }

        if (wanted.Count > MaxCookbookRecipesAtOnce)
        {
            throw new RuleViolationException($"Add up to {MaxCookbookRecipesAtOnce} recipes at a time.");
        }

        var found = new List<Recipe>();
        foreach (var id in wanted)
        {
            var recipe = await recipes.FindAsync(id, cancellationToken);
            if (recipe is null || recipe.IsArchived)
            {
                throw new NotFoundException("One of those recipes is no longer in the library. Reload the page and choose again.");
            }

            found.Add(recipe);
        }

        return found;
    }

    /// <summary>
    /// Checks that the plan styles' cookbook rules name cookbooks that exist, so a rule never quietly matches nothing.
    /// </summary>
    public async Task CookbookRulesAsync(IReadOnlyList<PlanStyleDetails> styles, CancellationToken cancellationToken)
    {
        var named = styles.SelectMany(style => style.Rules ?? [])
            .Where(rule => rule.Match == RecipeMatchKind.Cookbook)
            .SelectMany(rule => rule.Values ?? [])
            .ToList();
        if (named.Count == 0)
        {
            return;
        }

        var known = (await cookbooks.ListAsync(cancellationToken)).Select(cookbook => cookbook.Id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!named.All(value => known.Contains(value.Trim())))
        {
            throw new NotFoundException("A cookbook rule names a cookbook that no longer exists. Reload the page and choose another.");
        }
    }
}
