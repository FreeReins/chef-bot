using System.Text.Json.Nodes;
using AngleSharp.Dom;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Reads schema.org recipes written as HTML microdata (<c>itemscope</c>, <c>itemtype</c>, <c>itemprop</c>) into the
/// same JSON objects a JSON-LD script would hold, so <see cref="SchemaRecipe"/> reads both the same way.
/// </summary>
internal static class Microdata
{
    /// <summary>Each top-level schema.org Recipe item on the page, as JSON text.</summary>
    public static IEnumerable<string> Recipes(IDocument page) =>
        page.QuerySelectorAll("[itemscope][itemtype]")
            .Where(element => IsRecipe(element.GetAttribute("itemtype")))
            .Where(element => !(element.ParentElement?.Closest("[itemscope][itemtype]") is { } outer && IsRecipe(outer.GetAttribute("itemtype"))))
            .Select(element => Item(element).ToJsonString());

    private static bool IsRecipe(string? itemType) =>
        (itemType ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(type => type.TrimEnd('/').EndsWith("schema.org/Recipe", StringComparison.OrdinalIgnoreCase));

    /// <summary>One item: its type, and each property's values. A property given more than once becomes a list.</summary>
    private static JsonObject Item(IElement scope)
    {
        var item = new JsonObject();
        if (scope.GetAttribute("itemtype") is { } itemType)
        {
            item["@type"] = itemType.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].TrimEnd('/').Split('/')[^1];
        }

        foreach (var property in Properties(scope))
        {
            foreach (var name in (property.GetAttribute("itemprop") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                JsonNode? value = property.HasAttribute("itemscope") ? Item(property) : JsonValue.Create(Value(property));
                switch (item[name])
                {
                    case null:
                        item[name] = value;
                        break;
                    case JsonArray list:
                        list.Add(value);
                        break;
                    case var first:
                        item.Remove(name);
                        item[name] = new JsonArray(first, value);
                        break;
                }
            }
        }

        return item;
    }

    /// <summary>The elements with an <c>itemprop</c> that belong to this item, not to an item nested inside it.</summary>
    private static IEnumerable<IElement> Properties(IElement scope)
    {
        foreach (var child in scope.Children)
        {
            if (child.HasAttribute("itemprop"))
            {
                yield return child;
            }

            if (!child.HasAttribute("itemscope"))
            {
                foreach (var nested in Properties(child))
                {
                    yield return nested;
                }
            }
        }
    }

    /// <summary>A property's value, from the attribute HTML defines for its element, or else its text.</summary>
    private static string Value(IElement element)
    {
        var attribute = element.LocalName switch
        {
            "meta" => "content",
            "a" or "area" or "link" => "href",
            "img" or "audio" or "video" or "source" or "embed" or "iframe" or "track" => "src",
            "object" => "data",
            "time" => "datetime",
            "data" or "meter" => "value",
            _ => null,
        };
        return (attribute is null ? null : element.GetAttribute(attribute)) ?? element.TextContent;
    }
}
