using ChefBot.Api.Https;
using ChefBot.Core.Contracts;
using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Contracts.Services;

namespace ChefBot.Api.Messaging;

// The message bus's clients. Like the HTTP endpoints, each handler calls one manager and reports the outcome;
// the managers publish the commands and events these handle. Expected problems (an empty recipe library,
// Mealie being down, a webhook that doesn't answer) are logged here, not retried.

/// <summary>Stages next week's plan when the weekly schedule's time has come.</summary>
public static class CheckWeeklyScheduleHandler
{
    public static async Task Handle(CheckWeeklySchedule command, IPlanManager plans, ILogger<CheckWeeklySchedule> logger, CancellationToken cancellationToken)
    {
        try
        {
            await plans.CheckWeeklyScheduleAsync(cancellationToken);
        }
        catch (ChefBotException exception)
        {
            // For example an empty recipe library. Next week gets another try.
            logger.LogWarning("The weekly plan couldn't be staged: {Reason}", exception.Message);
        }
    }
}

/// <summary>Stages the scheduled week once the Mealie import that came first has finished.</summary>
public static class StageScheduledWeekHandler
{
    public static async Task Handle(StageScheduledWeek command, IPlanManager plans, ILogger<StageScheduledWeek> logger, CancellationToken cancellationToken)
    {
        try
        {
            await plans.StageScheduledWeekAsync(command, cancellationToken);
        }
        catch (ChefBotException exception)
        {
            logger.LogWarning("The weekly plan couldn't be staged: {Reason}", exception.Message);
        }
    }
}

/// <summary>Brings the open plans' shopping lists in line with a pantry that was edited by hand.</summary>
public static class PantryChangedHandler
{
    public static Task Handle(PantryChanged message, IPlanManager plans, CancellationToken cancellationToken) =>
        plans.RefreshOpenPlansAsync(cancellationToken);
}

/// <summary>Announces a newly staged plan through the notification webhook.</summary>
public static class PlanStagedHandler
{
    public static async Task Handle(PlanStaged message, IMaintenanceManager maintenance, ILogger<PlanStaged> logger, CancellationToken cancellationToken)
    {
        var run = await maintenance.SendPlanReadyAsync(message, cancellationToken);
        if (run.Problems.Count > 0)
        {
            logger.LogWarning("The plan-ready notification wasn't delivered: {Problems}", string.Join("; ", run.Problems));
        }
    }
}

/// <summary>Sends the daily reminders when their time has come.</summary>
public static class SendDueRemindersHandler
{
    public static async Task Handle(SendDueReminders command, IMaintenanceManager maintenance, ILogger<SendDueReminders> logger, CancellationToken cancellationToken)
    {
        var run = await maintenance.SendDueRemindersAsync(cancellationToken);
        if (run is { Problems.Count: > 0 })
        {
            logger.LogWarning("Some reminders weren't delivered: {Problems}", string.Join("; ", run.Problems));
        }
    }
}

/// <summary>Runs the daily backup when its time has come.</summary>
public static class RunDueBackupHandler
{
    public static async Task Handle(RunDueBackup command, IMaintenanceManager maintenance, ILogger<RunDueBackup> logger, CancellationToken cancellationToken)
    {
        try
        {
            if (await maintenance.RunDueBackupAsync(cancellationToken) is { } backup)
            {
                logger.LogInformation("Backup written: {Name}", backup.Name);
            }
        }
        catch (ChefBotException exception)
        {
            // Already recorded where an admin can see it; the next check tries again tomorrow.
            logger.LogWarning("The scheduled backup had a problem: {Reason}", exception.Message);
        }
    }
}

/// <summary>Runs a Mealie import in the background.</summary>
public static class ImportMealieRecipesHandler
{
    public static async Task Handle(ImportMealieRecipes command, IRecipeManager recipes, ImportStatusTracker status, TimeProvider clock, CancellationToken cancellationToken)
    {
        status.Set(new ImportStatus("Running", clock.GetUtcNow()));
        await recipes.ImportFromMealieAsync(command, cancellationToken);
    }
}

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

/// <summary>Applies a changed "always use HTTPS" setting to the running web server.</summary>
public static class SecuritySettingsChangedHandler
{
    public static void Handle(SecuritySettingsChanged message, HttpsPolicy policy) => policy.RequireHttps = message.RequireHttps;
}
