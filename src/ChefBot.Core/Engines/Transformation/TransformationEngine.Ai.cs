using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Transformation;

/// <content>Questions for AI models, and reading their answers.</content>
internal sealed partial class TransformationEngine
{
    /// <summary>A short question that only checks a model answers, and answers in JSON.</summary>
    public static AiQuestion AiTestQuestion() => new(
        "You are checking that a connection works. Answer only with the JSON asked for.",
        "Set ok to true.",
        [],
        """{"type":"object","properties":{"ok":{"type":"boolean"}},"required":["ok"],"additionalProperties":false}""",
        "connection_check");

    /// <summary>The question that reads a recipe from photos, the pages in order.</summary>
    public static AiQuestion RecipePhotoQuestion(IReadOnlyList<AiImage> photos) =>
        new(RecipePhotoPrompt.Instructions, RecipePhotoPrompt.Question, photos, RecipePhotoPrompt.Schema, RecipePhotoPrompt.SchemaName);

    /// <summary>
    /// The recipe a model read from photos, sorted into a course with the household's words when the model didn't
    /// name a known one. Null when it found no recipe: it said so, or gave neither a name nor any ingredients.
    /// </summary>
    public static RecipeText? ReadPhotoAnswer(string json, ClassifierWords words)
    {
        if (AiAnswer.Parse(json) is not { } answer
            || (answer.TryGetProperty("found", out var found) && found.ValueKind == System.Text.Json.JsonValueKind.False))
        {
            return null;
        }

        var name = AiAnswer.Text(answer, "name");
        var ingredients = AiAnswer.Lines(answer, "ingredients");
        if (name is null && ingredients.Count == 0)
        {
            return null;
        }

        var tags = AiAnswer.Lines(answer, "tags");
        var (classified, slowCooker) = RecipeClassifier.Classify(name ?? string.Empty, tags, words);
        var course = Enum.TryParse<RecipeCourse>(AiAnswer.Text(answer, "course"), true, out var named) && Enum.IsDefined(named) ? named : classified;
        var instructions = AiAnswer.Lines(answer, "instructions");
        return new RecipeText(
            name ?? string.Empty,
            course,
            ingredients,
            slowCooker || AiAnswer.Flag(answer, "slowCooker"),
            AiAnswer.Number(answer, "servings"),
            AiAnswer.Text(answer, "description"),
            instructions.Count > 0 ? instructions : null,
            AiAnswer.Minutes(answer, "prepMinutes"),
            AiAnswer.Minutes(answer, "cookMinutes"),
            AiAnswer.Minutes(answer, "totalMinutes"),
            null,
            AiAnswer.Text(answer, "author"),
            AiAnswer.Text(answer, "yield"),
            AiAnswer.Text(answer, "notes"),
            tags.Count > 0 ? tags : null);
    }
}
