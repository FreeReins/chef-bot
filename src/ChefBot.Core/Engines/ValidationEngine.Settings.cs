using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <content>Checks on the sign-in, webhook, backup and schedule settings.</content>
internal sealed partial class ValidationEngine
{
    public static void SignInRules(SecurityRequest request)
    {
        if (request.MaxFailedSignIns is < 0 or > 100)
        {
            throw new RuleViolationException("Failed attempts must be between 0 and 100. Zero turns lockout off.");
        }

        if (request.LockoutMinutes is < 1 or > 1440)
        {
            throw new RuleViolationException("The lockout must last between 1 minute and 24 hours.");
        }
    }

    /// <summary>The webhook address to save: empty, or a full http or https address.</summary>
    public static string Webhook(string? url)
    {
        var trimmed = url?.Trim() ?? string.Empty;
        return trimmed.Length == 0 || WebAddress.IsHttp(trimmed)
            ? trimmed
            : throw new RuleViolationException("The webhook must be a full http or https address.");
    }

    public static void BackupLimits(int keepCount, int keepDays)
    {
        if (keepCount is < 0 or > 1000 || keepDays is < 0 or > 3650)
        {
            throw new RuleViolationException("Keep between 0 and 1000 backups, for between 0 and 3650 days. Zero means no limit.");
        }
    }

    /// <summary>A time of day in its saved form, HH:mm.</summary>
    public static string TimeOfDay(string? time) => DailyTime.Format(DailyTime.Parse(time));
}
