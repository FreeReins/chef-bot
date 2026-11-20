namespace ChefBot.Core.Common;

public static class ClockExtensions
{
    /// <summary>Today's date in the server's time zone (set with the TZ environment variable).</summary>
    public static DateOnly Today(this TimeProvider clock) => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
}
