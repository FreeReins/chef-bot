namespace ChefBot.Core.ResourceAccess.Contracts;

/// <summary>The kinds of notification ChefBot sends. The value is the <c>type</c> field of the webhook payload.</summary>
internal static class NotificationTypes
{
    /// <summary>A new plan was staged and is waiting for review.</summary>
    public const string PlanReady = "plan_ready";

    /// <summary>Tonight's dinner is a slow cooker meal that needs starting.</summary>
    public const string SlowCooker = "slow_cooker";

    /// <summary>What is planned for today.</summary>
    public const string MealsToday = "meals_today";

    /// <summary>Food in the pantry is about to expire.</summary>
    public const string ExpiringFood = "expiring_food";

    /// <summary>The week is about to start and the plan is unconfirmed or has groceries left to buy.</summary>
    public const string Shopping = "shopping";

    /// <summary>Sent from the Settings tab to check the webhook works.</summary>
    public const string Test = "test";
}
