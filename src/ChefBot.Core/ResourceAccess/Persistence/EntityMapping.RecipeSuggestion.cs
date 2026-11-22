using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies RecipeSuggestionEntity to and from RecipeSuggestion.</content>
internal static partial class EntityMapping
{
    public static RecipeSuggestion ToContract(this RecipeSuggestionEntity entity) => new()
    {
        Id = entity.Id,
        RecipeId = entity.RecipeId,
        BasedOn = entity.BasedOn,
        ModelName = entity.ModelName,
        CreatedAt = entity.CreatedAt,
        Status = entity.Status,
        Changes = JsonSerializer.Deserialize(entity.Changes, ChangeJson.Default.ListSuggestedChange) ?? [],
    };

    public static RecipeSuggestionEntity ToEntity(this RecipeSuggestion suggestion) => new()
    {
        Id = suggestion.Id,
        RecipeId = suggestion.RecipeId,
        BasedOn = suggestion.BasedOn,
        ModelName = suggestion.ModelName,
        CreatedAt = suggestion.CreatedAt,
        Status = suggestion.Status,
        Changes = JsonSerializer.Serialize(suggestion.Changes, ChangeJson.Default.ListSuggestedChange),
    };

    /// <summary>The JSON shape of a suggestion's changes, as stored in its row, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(List<SuggestedChange>))]
    private sealed partial class ChangeJson : JsonSerializerContext;
}
