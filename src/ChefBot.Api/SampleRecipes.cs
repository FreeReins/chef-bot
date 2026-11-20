using System.Text.Json;
using ChefBot.Core.Recipes;

namespace ChefBot.Api;

/// <summary>The starter recipes offered by the setup wizard, read from config/sample-recipes.json.</summary>
public sealed class SampleRecipes(string path)
{
    private readonly Lazy<IReadOnlyList<RecipeDraft>> _all = new(() => Load(path));

    public IReadOnlyList<RecipeDraft> All => _all.Value;

    private static IReadOnlyList<RecipeDraft> Load(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        return JsonSerializer.Deserialize<List<RecipeDraft>>(File.ReadAllText(path), options) ?? [];
    }
}
