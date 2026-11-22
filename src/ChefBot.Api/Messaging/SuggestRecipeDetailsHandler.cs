using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>
/// Asks the AI model what one recipe is missing, for the library sweep. A model that is busy or unreachable is tried
/// again later by the error policies; one that refuses, or is no longer set up, is logged and the recipe skipped.
/// </summary>
[StickyHandler(MessageQueues.Ai)]
public static class SuggestRecipeDetailsHandler
{
    public static async Task Handle(
        SuggestRecipeDetails command, IRecipeManager recipes, ILogger<SuggestRecipeDetails> logger, CancellationToken cancellationToken)
    {
        try
        {
            await recipes.SuggestDetailsForSweepAsync(command, cancellationToken);
        }
        catch (AiUnavailableException exception) when (!exception.IsTransient)
        {
            logger.LogWarning("No suggestions for recipe {RecipeId}: {Problem}", command.RecipeId, exception.Message);
        }
        catch (RuleViolationException exception)
        {
            logger.LogWarning("No suggestions for recipe {RecipeId}: {Problem}", command.RecipeId, exception.Message);
        }
    }
}
