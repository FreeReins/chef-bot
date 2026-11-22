using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Engines.Contracts.Services;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using static ChefBot.Core.Engines.Transformation.JsonValues;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// The general recipe JSON format: ChefBot's own shape, a Mealie recipe export or schema.org, as
/// <see cref="SchemaRecipe"/> reads them. The file may hold one recipe, a list of them, <c>{ "recipes": [...] }</c>
/// or a JSON-LD <c>@graph</c>.
/// </summary>
internal sealed class JsonRecipeFormat(SchemaRecipe schema) : IRecipeFileFormat
{
    public string Name => "Recipe JSON (ChefBot, a Mealie recipe export or schema.org)";

    public IReadOnlyList<string> Extensions { get; } = [".json"];

    /// <summary>Always true: this is the fallback for any .json file no more specific format recognises.</summary>
    public bool CanRead(string text) => true;

    /// <exception cref="RuleViolationException">The file isn't JSON or holds no recipes at all.</exception>
    public RecipeFile Read(string text, ClassifierWords words)
    {
        using var document = Parse(text, "JSON recipe");
        var entries = SchemaRecipe.Entries(document.RootElement).ToList();
        if (entries.Count == 0)
        {
            throw new RuleViolationException("No recipes were found in that file. Each recipe needs a name and a list of ingredients.");
        }

        var recipes = entries.Select(entry => schema.Read(entry, words)).OfType<RecipeText>().ToList();
        return new RecipeFile(recipes, entries.Count - recipes.Count);
    }
}
