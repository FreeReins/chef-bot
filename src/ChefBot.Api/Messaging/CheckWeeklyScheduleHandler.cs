using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;

namespace ChefBot.Api.Messaging;

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
