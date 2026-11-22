using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <content>The meal plan's calendar feed.</content>
internal sealed partial class PlanManager
{
    public async Task<CalendarFeedResponse> GetCalendarFeedAsync(Uri site, CancellationToken cancellationToken) =>
        Describe(await settings.GetCalendarFeedAsync(cancellationToken), site);

    public async Task<CalendarFeedResponse> StartCalendarFeedAsync(Uri site, CancellationToken cancellationToken)
    {
        var feed = new CalendarFeed { Token = CalendarEngine.NewToken(), CreatedAt = clock.GetUtcNow() };
        await settings.SaveCalendarFeedAsync(feed, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return Describe(feed, site);
    }

    public async Task<CalendarFeedResponse> StopCalendarFeedAsync(CancellationToken cancellationToken)
    {
        await settings.SaveCalendarFeedAsync(new CalendarFeed(), cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return new CalendarFeedResponse(null, null);
    }

    public async Task<string> GetCalendarAsync(string token, Uri site, CancellationToken cancellationToken)
    {
        if (!CalendarEngine.Matches(await settings.GetCalendarFeedAsync(cancellationToken), token))
        {
            throw new NotFoundException("There is no calendar at that address. Copy the address again from ChefBot's settings.");
        }

        var weeks = await plans.ListOpenAsync(clock.Today().AddDays(-7), cancellationToken);
        var times = await settings.GetCalendarTimesAsync(cancellationToken);
        return calendar.Write(weeks.Where(plan => plan.Status == PlanStatus.Confirmed), site, clock.GetUtcNow(), times);
    }

    private static CalendarFeedResponse Describe(CalendarFeed feed, Uri site) => feed.Token.Length == 0
        ? new CalendarFeedResponse(null, null)
        : new CalendarFeedResponse(new Uri(site, $"api/calendar/{feed.Token}/meals.ics").AbsoluteUri, feed.CreatedAt);
}
