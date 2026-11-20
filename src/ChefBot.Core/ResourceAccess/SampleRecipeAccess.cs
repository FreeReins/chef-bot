using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.ResourceAccess;

/// <summary>The starter recipes offered by the setup wizard, read once from config/sample-recipes.json.</summary>
internal sealed partial class SampleRecipeAccess(string path) : ISampleRecipeAccess
{
    private readonly Lazy<IReadOnlyList<RecipeText>> _all = new(() => Load(path));

    public IReadOnlyList<RecipeText> All() => _all.Value;

    private static IReadOnlyList<RecipeText> Load(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        return JsonSerializer.Deserialize(File.ReadAllText(path), SampleJson.Default.ListRecipeText) ?? [];
    }

    /// <summary>The sample recipe file's JSON shape, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(List<RecipeText>))]
    private sealed partial class SampleJson : JsonSerializerContext;
}
