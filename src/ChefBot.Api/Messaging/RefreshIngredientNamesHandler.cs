using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>
/// Files saved recipes' ingredients under the catalog's current names. Running it twice changes nothing the second
/// time.
/// </summary>
[StickyHandler(MessageQueues.Maintenance)]
public static class RefreshIngredientNamesHandler
{
    public static async Task Handle(
        RefreshIngredientNames message, IRecipeManager recipes, ILogger<RefreshIngredientNames> logger, CancellationToken cancellationToken)
    {
        var changed = await recipes.RefreshIngredientNamesAsync(message, cancellationToken);
        if (changed > 0)
        {
            logger.LogInformation("The ingredients of {Count} recipes were filed under the catalog's current names.", changed);
        }
    }
}
