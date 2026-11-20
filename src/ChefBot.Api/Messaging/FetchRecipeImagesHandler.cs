using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;

namespace ChefBot.Api.Messaging;

/// <summary>Downloads an imported recipe's photos in the background, so imports don't wait for them.</summary>
public static class FetchRecipeImagesHandler
{
    public static async Task Handle(FetchRecipeImages command, IRecipeManager recipes, ILogger<FetchRecipeImages> logger, CancellationToken cancellationToken)
    {
        // A photo that can't be downloaded is expected now and then; the recipe works without it.
        var problems = await recipes.FetchImagesAsync(command, cancellationToken);
        if (problems.Count > 0)
        {
            logger.LogWarning("Some recipe photos weren't downloaded: {Problems}", string.Join("; ", problems));
        }
    }
}
