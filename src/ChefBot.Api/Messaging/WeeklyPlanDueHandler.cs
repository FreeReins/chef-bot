using ChefBot.Core.Common;
using ChefBot.Core.Planning;
using Wolverine;

namespace ChefBot.Api.Messaging;

/// <summary>Stages next week's plan when the weekly schedule fires, then books the following week.</summary>
public sealed class WeeklyPlanDueHandler
{
    public async Task Handle(
        WeeklyPlanDue message,
        PlanningService planning,
        WeeklyScheduleOptions schedule,
        TimeProvider clock,
        IMessageBus bus,
        ILogger<WeeklyPlanDueHandler> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var staged = await planning.StageUpcomingIfMissingAsync(cancellationToken);
            if (staged is not null)
            {
                await bus.PublishAsync(new PlanStaged(staged.Plan.Id, "schedule"));
            }
        }
        catch (ChefBotException exception)
        {
            // For example an empty recipe library. Next week gets another try.
            logger.LogWarning("The weekly plan couldn't be staged: {Reason}", exception.Message);
        }
        finally
        {
            await bus.ScheduleAsync(new WeeklyPlanDue(), schedule.NextOccurrence(clock.GetLocalNow()));
        }
    }
}
