using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;

namespace ChefBot.Api.Messaging;

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
