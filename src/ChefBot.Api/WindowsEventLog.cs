using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.EventLog;

namespace ChefBot.Api;

/// <summary>The Windows service's warnings and errors go to the Application event log as ChefBot, the source the installer registers.</summary>
[SupportedOSPlatform("windows")]
public static class WindowsEventLog
{
    public const string Source = "ChefBot";

    public static void UseChefBotSource(EventLogSettings eventLog) => eventLog.SourceName = Source;
}
