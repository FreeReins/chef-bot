namespace ChefBot.Core.Utilities;

/// <summary>
/// The words that sort imported recipes into courses. They are matched as whole
/// words against a recipe's tags, categories and tools. Values come from appsettings.json.
/// </summary>
internal sealed class ClassifierOptions
{
    public List<string> BreakfastWords { get; set; } = [];

    public List<string> SnackWords { get; set; } = [];

    public List<string> SweetWords { get; set; } = [];

    /// <summary>Also matched against the recipe name.</summary>
    public List<string> SlowCookerWords { get; set; } = [];

    /// <summary>Labels for things that are never a meal on their own, such as sides and drinks.</summary>
    public List<string> NotAMealWords { get; set; } = [];

    /// <summary>When set, only recipes carrying one of these labels count as mains.</summary>
    public List<string> MainWords { get; set; } = [];
}
