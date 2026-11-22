using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>Decides which course an imported recipe belongs to from its labels, using the household's course words.</summary>
internal static class RecipeClassifier
{
    public static (RecipeCourse Course, bool IsSlowCooker) Classify(string name, IEnumerable<string> labels, ClassifierWords words)
    {
        var padded = labels.Select(Pad).ToList();
        var isSlowCooker = ContainsAny(padded.Append(Pad(name)).ToList(), words.SlowCookerWords);
        return (CourseOf(padded, words), isSlowCooker);
    }

    private static RecipeCourse CourseOf(IReadOnlyCollection<string> labels, ClassifierWords words)
    {
        if (ContainsAny(labels, words.BreakfastWords)) return RecipeCourse.Breakfast;
        if (ContainsAny(labels, words.SnackWords)) return RecipeCourse.Snack;
        if (ContainsAny(labels, words.SweetWords)) return RecipeCourse.Sweet;
        if (ContainsAny(labels, words.DrinkWords)) return RecipeCourse.Drink;
        if (ContainsAny(labels, words.NotAMealWords)) return RecipeCourse.Other;
        if (words.MainWords.Count > 0 && !ContainsAny(labels, words.MainWords)) return RecipeCourse.Other;
        return RecipeCourse.Main;
    }

    /// <summary>Surrounds normalized text with spaces so whole words can be found with a plain search.</summary>
    private static string Pad(string text) => $" {TextNormalizer.Normalize(text)} ";

    private static bool ContainsAny(IReadOnlyCollection<string> paddedLabels, IEnumerable<string> words) =>
        words.Where(word => !string.IsNullOrWhiteSpace(word))
            .Select(Pad)
            .Any(word => paddedLabels.Any(label => label.Contains(word, StringComparison.Ordinal)));
}
