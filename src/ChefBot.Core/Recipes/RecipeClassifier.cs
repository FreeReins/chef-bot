using ChefBot.Core.Common;

namespace ChefBot.Core.Recipes;

/// <summary>
/// The words that sort imported recipes into courses. They are matched as whole
/// words against a recipe's tags, categories and tools. Values come from appsettings.json.
/// </summary>
public sealed class ClassifierOptions
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

/// <summary>Decides which course an imported recipe belongs to from its labels.</summary>
public sealed class RecipeClassifier(ClassifierOptions options)
{
    public (RecipeCourse Course, bool IsSlowCooker) Classify(string name, IEnumerable<string> labels)
    {
        var padded = labels.Select(Pad).ToList();
        var isSlowCooker = ContainsAny(padded.Append(Pad(name)).ToList(), options.SlowCookerWords);
        return (CourseOf(padded), isSlowCooker);
    }

    private RecipeCourse CourseOf(IReadOnlyCollection<string> labels)
    {
        if (ContainsAny(labels, options.BreakfastWords)) return RecipeCourse.Breakfast;
        if (ContainsAny(labels, options.SnackWords)) return RecipeCourse.Snack;
        if (ContainsAny(labels, options.SweetWords)) return RecipeCourse.Sweet;
        if (ContainsAny(labels, options.NotAMealWords)) return RecipeCourse.Other;
        if (options.MainWords.Count > 0 && !ContainsAny(labels, options.MainWords)) return RecipeCourse.Other;
        return RecipeCourse.Main;
    }

    /// <summary>Surrounds normalized text with spaces so whole words can be found with a plain search.</summary>
    private static string Pad(string text) => $" {TextNormalizer.Normalize(text)} ";

    private static bool ContainsAny(IReadOnlyCollection<string> paddedLabels, IEnumerable<string> words) =>
        words.Where(word => !string.IsNullOrWhiteSpace(word))
            .Select(Pad)
            .Any(word => paddedLabels.Any(label => label.Contains(word, StringComparison.Ordinal)));
}
