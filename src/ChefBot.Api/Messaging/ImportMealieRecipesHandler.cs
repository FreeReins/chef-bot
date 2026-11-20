using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;

namespace ChefBot.Api.Messaging;

/// <summary>Runs a Mealie import in the background.</summary>
public static class ImportMealieRecipesHandler
{
    public static async Task Handle(ImportMealieRecipes command, IRecipeManager recipes, ImportStatusTracker status, TimeProvider clock, CancellationToken cancellationToken)
    {
        status.Set(new ImportStatus("Running", clock.GetUtcNow()));
        await recipes.ImportFromMealieAsync(command, cancellationToken);
    }
}
