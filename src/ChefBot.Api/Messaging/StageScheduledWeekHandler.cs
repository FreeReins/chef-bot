using ChefBot.Core.Contracts;
using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Contracts.Services;

namespace ChefBot.Api.Messaging;

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
