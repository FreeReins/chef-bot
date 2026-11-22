using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>Suggestions from an AI model for what recipes are missing.</content>
internal sealed partial class RecipeManager
{
    public async Task<RecipeSuggestionResponse> SuggestDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await ActiveAsync(id, cancellationToken);
        var model = ValidationEngine.AiModelFor(AiTask.RecipeDetails, await settings.GetAiAsync(cancellationToken));

        var suggestion = await SuggestAsync(recipe, model, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return suggestion.ToResponse(stale: false);
    }

    public async Task<RecipeSuggestionResponse> GetSuggestionAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await ActiveAsync(id, cancellationToken);
        var suggestion = ValidationEngine.Suggestion(await suggestions.FindAsync(recipe.Id, cancellationToken));
        return suggestion.ToResponse(stale: suggestion.BasedOn != RecipeCompleteness.Fingerprint(recipe));
    }

    public async Task<RecipeResponse> ApplySuggestionAsync(Guid id, ApplySuggestionRequest request, CancellationToken cancellationToken)
    {
        var recipe = await ActiveAsync(id, cancellationToken);
        var suggestion = ValidationEngine.Suggestion(await suggestions.FindAsync(recipe.Id, cancellationToken));
        var chosen = ValidationEngine.ChangesToApply(suggestion, RecipeCompleteness.Fingerprint(recipe), request.ChangeIds);

        var text = RecipeCompleteness.Apply(RecipeCompleteness.TextOf(recipe), chosen);
        ValidationEngine.Recipe(text);
        transformation.ApplyText(text, recipe, clock.GetUtcNow());
        suggestion.Status = RecipeSuggestionStatus.Applied;
        suggestion.BasedOn = RecipeCompleteness.Fingerprint(recipe);
        await recipes.SaveAsync(recipe, cancellationToken);
        await suggestions.SaveAsync(suggestion, cancellationToken);
        await messages.PublishAsync(new RecipeChanged(recipe.Id));
        await messages.PublishAsync(new RecipeSuggestionsChanged(recipe.Id));
        await messages.CommitAsync(cancellationToken);
        return await ResponseAsync(recipe, cancellationToken);
    }

    public async Task DismissSuggestionAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await ActiveAsync(id, cancellationToken);
        if (await suggestions.FindAsync(recipe.Id, cancellationToken) is not { Status: RecipeSuggestionStatus.Waiting } suggestion)
        {
            return;
        }

        suggestion.Status = RecipeSuggestionStatus.Dismissed;
        await suggestions.SaveAsync(suggestion, cancellationToken);
        await messages.PublishAsync(new RecipeSuggestionsChanged(recipe.Id));
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<DetailsSweepStatusResponse> GetDetailsSweepAsync(CancellationToken cancellationToken)
    {
        var model = ValidationEngine.AiModelOrNull(AiTask.RecipeDetails, await settings.GetAiAsync(cancellationToken));
        var library = await recipes.ListActiveAsync(cancellationToken);
        var saved = (await suggestions.ListAllAsync(cancellationToken)).ToDictionary(suggestion => suggestion.RecipeId);
        var waiting = library.Count(recipe => saved.TryGetValue(recipe.Id, out var suggestion)
            && suggestion.Status == RecipeSuggestionStatus.Waiting && suggestion.BasedOn == RecipeCompleteness.Fingerprint(recipe));
        return new DetailsSweepStatusResponse(SweepCandidates(library, saved).Count, waiting, model?.Name);
    }

    public async Task<DetailsSweepResponse> StartDetailsSweepAsync(CancellationToken cancellationToken)
    {
        var model = ValidationEngine.AiModelFor(AiTask.RecipeDetails, await settings.GetAiAsync(cancellationToken));
        var library = await recipes.ListActiveAsync(cancellationToken);
        var saved = (await suggestions.ListAllAsync(cancellationToken)).ToDictionary(suggestion => suggestion.RecipeId);

        var queued = SweepCandidates(library, saved);
        foreach (var recipe in queued)
        {
            await messages.PublishAsync(new SuggestRecipeDetails(recipe.Id));
        }

        await messages.CommitAsync(cancellationToken);
        return new DetailsSweepResponse(queued.Count, model.Name);
    }

    public async Task<bool> SuggestDetailsForSweepAsync(SuggestRecipeDetails command, CancellationToken cancellationToken)
    {
        if (await recipes.FindAsync(command.RecipeId, cancellationToken) is not { IsArchived: false } recipe
            || RecipeCompleteness.Gaps(recipe).Count == 0
            || (await suggestions.FindAsync(recipe.Id, cancellationToken))?.BasedOn == RecipeCompleteness.Fingerprint(recipe))
        {
            return false;
        }

        var model = ValidationEngine.AiModelFor(AiTask.RecipeDetails, await settings.GetAiAsync(cancellationToken));
        await SuggestAsync(recipe, model, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>Recipes missing details that have no suggestion for how they are now, whatever became of it.</summary>
    private static List<Recipe> SweepCandidates(IReadOnlyList<Recipe> library, IReadOnlyDictionary<Guid, RecipeSuggestion> saved) =>
        library
            .Where(recipe => RecipeCompleteness.Gaps(recipe).Count > 0)
            .Where(recipe => !saved.TryGetValue(recipe.Id, out var suggestion) || suggestion.BasedOn != RecipeCompleteness.Fingerprint(recipe))
            .ToList();

    /// <summary>Asks the model what the recipe is missing and keeps what it suggests, replacing any earlier suggestion.</summary>
    private async Task<RecipeSuggestion> SuggestAsync(Recipe recipe, AiModel model, CancellationToken cancellationToken)
    {
        var answer = await ai.AskAsync(model, RecipeCompleteness.Question(recipe), cancellationToken);
        var changes = RecipeCompleteness.Changes(recipe, answer);
        var suggestion = new RecipeSuggestion
        {
            RecipeId = recipe.Id,
            BasedOn = RecipeCompleteness.Fingerprint(recipe),
            ModelName = model.Name,
            CreatedAt = clock.GetUtcNow(),
            Status = changes.Count > 0 ? RecipeSuggestionStatus.Waiting : RecipeSuggestionStatus.NothingToAdd,
            Changes = changes,
        };
        await suggestions.SaveAsync(suggestion, cancellationToken);
        await messages.PublishAsync(new RecipeSuggestionsChanged(recipe.Id));
        return suggestion;
    }

    /// <summary>Whether each of the recipes has suggestions waiting for how it is now.</summary>
    private async Task<Func<Recipe, bool>> SuggestedAsync(IReadOnlyCollection<Recipe> shown, CancellationToken cancellationToken)
    {
        var waiting = (await suggestions.ListForRecipesAsync(shown.Select(recipe => recipe.Id).ToList(), cancellationToken))
            .Where(suggestion => suggestion.Status == RecipeSuggestionStatus.Waiting)
            .ToDictionary(suggestion => suggestion.RecipeId, suggestion => suggestion.BasedOn);
        return recipe => waiting.TryGetValue(recipe.Id, out var basedOn) && basedOn == RecipeCompleteness.Fingerprint(recipe);
    }
}
