using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Api.Messaging;

/// <summary>Records how an import went, and stages the scheduled week that was waiting for it.</summary>
public static class RecipeImportFinishedHandler
{
    public static StageScheduledWeek? Handle(
        RecipeImportFinished message, ImportStatusTracker status, TimeProvider clock, ILogger<RecipeImportFinished> logger)
    {
        if (message.Succeeded)
        {
            status.Set(new ImportStatus("Succeeded", clock.GetUtcNow(), Result: message.Result));
            logger.LogInformation("Mealie import finished: {Result}", message.Result);
        }
        else
        {
            status.Set(new ImportStatus("Failed", clock.GetUtcNow(), message.Problem));
            logger.LogWarning("Mealie import failed: {Reason}", message.Problem);
        }

        // Cascades: Wolverine sends the returned message, if any.
        return message.StageWeekAfter is DateOnly week ? new StageScheduledWeek(week) : null;
    }
}
