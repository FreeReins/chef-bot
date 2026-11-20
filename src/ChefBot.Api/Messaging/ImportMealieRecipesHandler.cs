using ChefBot.Core.Common;
using ChefBot.Core.Recipes;

namespace ChefBot.Api.Messaging;

/// <summary>Runs a Mealie import in the background and records how it went.</summary>
public sealed class ImportMealieRecipesHandler
{
    public async Task Handle(
        ImportMealieRecipes command,
        RecipeImportService imports,
        ImportStatusTracker status,
        TimeProvider clock,
        ILogger<ImportMealieRecipesHandler> logger,
        CancellationToken cancellationToken)
    {
        status.Set(new ImportStatus("Running", clock.GetUtcNow()));
        try
        {
            var result = await imports.ImportAsync(cancellationToken);
            status.Set(new ImportStatus("Succeeded", clock.GetUtcNow(), Result: result));
            logger.LogInformation("Mealie import finished: {Result}", result);
        }
        catch (ChefBotException exception)
        {
            // An unreachable Mealie is an expected outcome, not a message to retry.
            status.Set(new ImportStatus("Failed", clock.GetUtcNow(), exception.Message));
            logger.LogWarning("Mealie import failed: {Reason}", exception.Message);
        }
    }
}
