using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <content>Checks on the sign-in, webhook, backup, planning, calendar and advanced settings.</content>
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

    /// <summary>The planning settings to save: people, the weekly schedule and leftovers.</summary>
    public static PlannerSettings Planning(PlannerSettingsRequest request)
    {
        if (request.People is < 1 or > 20)
        {
            throw new RuleViolationException("Plan for between 1 and 20 people.");
        }

        if (!Enum.IsDefined(request.ScheduleDay))
        {
            throw new RuleViolationException("Choose a day of the week for the weekly plan.");
        }

        if (request.MaxLeftoverDays is < 0 or > 7)
        {
            throw new RuleViolationException("Leftovers can keep for between 0 and 7 days. Zero plans no leftover lunches.");
        }

        return new PlannerSettings
        {
            People = request.People,
            ScheduleEnabled = request.ScheduleEnabled,
            ScheduleDay = request.ScheduleDay,
            ScheduleTime = TimeOfDay(request.ScheduleTime),
            MaxLeftoverDays = request.MaxLeftoverDays,
        };
    }

    /// <summary>The calendar feed's meal times to save.</summary>
    public static CalendarSettings CalendarTimes(CalendarTimesRequest request)
    {
        if (request.MealMinutes is < 5 or > 240)
        {
            throw new RuleViolationException("A meal's calendar event can last between 5 and 240 minutes.");
        }

        if (request.SlowCookerHours is < 0 or > 12)
        {
            throw new RuleViolationException("The slow cooker reminder can go off between 0 and 12 hours before dinner. Zero turns it off.");
        }

        return new CalendarSettings
        {
            BreakfastTime = TimeOfDay(request.BreakfastTime),
            LunchTime = TimeOfDay(request.LunchTime),
            DinnerTime = TimeOfDay(request.DinnerTime),
            MealMinutes = request.MealMinutes,
            SlowCookerHours = request.SlowCookerHours,
        };
    }

    /// <summary>The advanced settings to save, with anything left out set back to its default.</summary>
    public static AdvancedSettings Advanced(AdvancedSettingsRequest request)
    {
        var defaults = new AdvancedSettings();
        if (request.AvoidRepeatDays is < 0 or > 365)
        {
            throw new RuleViolationException("Recipes can be kept from repeating for between 0 and 365 days. Zero allows repeats.");
        }

        if (request.ExpiringSoonDays is < 0 or > 30)
        {
            throw new RuleViolationException("Expiring food can be planned first from between 0 and 30 days before it expires.");
        }

        if (request.PerishableWithinDays is < 1 or > 365)
        {
            throw new RuleViolationException("Ingredients worth sharing between meals must keep for between 1 and 365 days.");
        }

        var words = request.Words;
        return new AdvancedSettings
        {
            AvoidRepeatDays = request.AvoidRepeatDays ?? defaults.AvoidRepeatDays,
            ExpiringSoonDays = request.ExpiringSoonDays ?? defaults.ExpiringSoonDays,
            PerishableWithinDays = request.PerishableWithinDays ?? defaults.PerishableWithinDays,
            Words = new ClassifierWords
            {
                BreakfastWords = CourseWordList(words?.Breakfast, defaults.Words.BreakfastWords, "breakfast"),
                SnackWords = CourseWordList(words?.Snack, defaults.Words.SnackWords, "snack"),
                SweetWords = CourseWordList(words?.Sweet, defaults.Words.SweetWords, "sweet"),
                DrinkWords = CourseWordList(words?.Drink, defaults.Words.DrinkWords, "drink"),
                SlowCookerWords = CourseWordList(words?.SlowCooker, defaults.Words.SlowCookerWords, "slow cooker"),
                NotAMealWords = CourseWordList(words?.NotAMeal, defaults.Words.NotAMealWords, "not a meal"),
                MainWords = CourseWordList(words?.Main, defaults.Words.MainWords, "main"),
            },
        };
    }

    /// <summary>One course's words, trimmed, in lower case and without repeats; the default when not given.</summary>
    private static List<string> CourseWordList(IReadOnlyList<string>? words, List<string> fallback, string course)
    {
        if (words is null)
        {
            return fallback;
        }

        var cleaned = words
            .Select(word => word?.Trim().ToLowerInvariant() ?? string.Empty)
            .Where(word => word.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (cleaned.Count > 100 || cleaned.Any(word => word.Length > 50))
        {
            throw new RuleViolationException($"Keep the {course} words to 100 or fewer, each under 50 letters.");
        }

        return cleaned;
    }
}
