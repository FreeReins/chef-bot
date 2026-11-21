using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>Decides which course an imported recipe belongs to from its labels.</summary>
internal sealed class RecipeClassifier(ClassifierOptions options)
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
        if (ContainsAny(labels, options.DrinkWords)) return RecipeCourse.Drink;
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
