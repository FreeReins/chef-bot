namespace ChefBot.Api.Live;

/// <summary>The topics <see cref="LiveUpdates"/> sends, each matching what one part of the page loads.</summary>
public static class LiveTopics
{
    /// <summary>The week's plan, its meals and its shopping list.</summary>
    public const string Plan = "plan";

    /// <summary>The shopping added by hand.</summary>
    public const string Shopping = "shopping";

    /// <summary>The pantry.</summary>
    public const string Pantry = "pantry";

    /// <summary>The recipe library, including a recipe's photos.</summary>
    public const string Recipes = "recipes";

    /// <summary>The kitchen achievements, including ones not seen yet.</summary>
    public const string Achievements = "achievements";

    /// <summary>The backups list, after a backup finished.</summary>
    public const string Backups = "backups";

    /// <summary>The food history.</summary>
    public const string History = "history";
}
