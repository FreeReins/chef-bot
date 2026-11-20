namespace ChefBot.Api.Messaging;

/// <summary>Command: copy recipes from Mealie into the local database. Runs in the background.</summary>
public sealed record ImportMealieRecipes;

/// <summary>Scheduled trigger: it is time to stage next week's plan.</summary>
public sealed record WeeklyPlanDue;

/// <summary>Event: a plan was staged and is ready for review.</summary>
/// <param name="PlanId">The staged plan.</param>
/// <param name="Trigger">What staged it: "schedule", "manual" or "regenerate".</param>
public sealed record PlanStaged(Guid PlanId, string Trigger);

/// <summary>
/// Scheduled trigger, every few minutes: send the daily reminders and run the daily backup
/// when their times have come. Checking often keeps both on time after a restart or a settings change.
/// </summary>
public sealed record MaintenanceTick;
