namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>How plans weigh repeats, expiring food and perishables, and the words that sort imported recipes. Admins change them.</summary>
internal sealed class AdvancedSettings
{
    public const int DefaultAvoidRepeatDays = 28;

    public const int DefaultExpiringSoonDays = 3;

    public const int DefaultPerishableWithinDays = 21;

    /// <summary>Avoid repeating a recipe planned within this many days.</summary>
    public int AvoidRepeatDays { get; set; } = DefaultAvoidRepeatDays;

    /// <summary>Pantry items expiring within this many days are pushed into the plan first.</summary>
    public int ExpiringSoonDays { get; set; } = DefaultExpiringSoonDays;

    /// <summary>Ingredients that keep no longer than this are worth sharing between meals.</summary>
    public int PerishableWithinDays { get; set; } = DefaultPerishableWithinDays;

    public ClassifierWords Words { get; set; } = new();

    /// <summary>False until these are first saved: until then they are the defaults. Not stored itself.</summary>
    public bool IsSaved { get; set; }
}
