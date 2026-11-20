using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.ResourceAccess;

/// <summary>The well-known stores, read once from config/stores.json.</summary>
internal sealed partial class KnownStoreAccess(string path) : IKnownStoreAccess
{
    private readonly Lazy<IReadOnlyList<KnownStore>> _all = new(() => Load(path));

    public IReadOnlyList<KnownStore> All() => _all.Value;

    /// <summary>Reads a file shaped like <c>{ "stores": [ { "name": "Costco", "kind": "WarehouseClub", ... } ] }</c>.</summary>
    public static IReadOnlyList<KnownStore> FromJson(string json) =>
        JsonSerializer.Deserialize(json, StoreJson.Default.StoreFile)?.Stores ?? [];

    private static IReadOnlyList<KnownStore> Load(string path) => File.Exists(path) ? FromJson(File.ReadAllText(path)) : [];

    /// <summary>The whole file.</summary>
    internal sealed record StoreFile(List<KnownStore>? Stores);

    /// <summary>The store file's JSON shape, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(StoreFile))]
    private sealed partial class StoreJson : JsonSerializerContext;
}
