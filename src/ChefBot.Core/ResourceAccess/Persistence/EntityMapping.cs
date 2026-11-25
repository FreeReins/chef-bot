using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <summary>Copies between the stored entities and the data contracts, in both directions.</summary>
internal static partial class EntityMapping
{
    /// <summary>
    /// Brings a tracked row in line with a freshly mapped one: copies its columns, then does the same for each
    /// child collection, adding new children and deleting ones that are gone.
    /// </summary>
    public static void CopyInto<TEntity>(this DbContext db, TEntity tracked, TEntity fresh)
        where TEntity : class =>
        db.Entry(tracked).CurrentValues.SetValues(fresh);

    /// <summary>Makes <paramref name="current"/> hold exactly the children in <paramref name="fresh"/>, matched by id.</summary>
    /// <param name="db">The context tracking <paramref name="current"/>.</param>
    /// <param name="current">The tracked collection.</param>
    /// <param name="fresh">The children as they should be.</param>
    /// <param name="id">Reads a child's id.</param>
    /// <param name="nested">Syncs a matched child's own children, if it has any.</param>
    public static void SyncChildren<TEntity>(
        this DbContext db, List<TEntity> current, IEnumerable<TEntity> fresh, Func<TEntity, Guid> id, Action<TEntity, TEntity>? nested = null)
        where TEntity : class
    {
        var wanted = fresh.ToList();
        var wantedIds = wanted.Select(id).ToHashSet();

        // Removing a child from its parent's collection deletes its row.
        current.RemoveAll(child => !wantedIds.Contains(id(child)));

        var existing = current.ToDictionary(id);
        foreach (var child in wanted)
        {
            if (existing.TryGetValue(id(child), out var match))
            {
                db.CopyInto(match, child);
                nested?.Invoke(match, child);
            }
            else
            {
                current.Add(child);
            }
        }
    }

    private static List<ShoppingAmount> ReadAmounts(string? json) =>
        string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize(json, AmountJson.Default.ListShoppingAmount) ?? [];

    private static string WriteAmounts(List<ShoppingAmount> amounts) => JsonSerializer.Serialize(amounts, AmountJson.Default.ListShoppingAmount);

    /// <summary>The JSON shape of a shopping line's extra amounts, as stored in its row, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(List<ShoppingAmount>))]
    private sealed partial class AmountJson : JsonSerializerContext;

    /// <summary>The JSON shape of a plan style's rules, as stored in its row, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(List<PlanRule>))]
    private sealed partial class RuleJson : JsonSerializerContext;
}
