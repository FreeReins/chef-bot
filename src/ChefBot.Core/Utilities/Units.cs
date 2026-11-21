namespace ChefBot.Core.Utilities;

/// <summary>
/// Gives units one spelling so "cups" and "Cup" add up, and converts between units that measure the same thing,
/// so 2 tbsp and ½ cup, or 8 oz and ½ lb, can be added and compared.
/// </summary>
internal static class Units
{
    private const string Volume = "volume";
    private const string Weight = "weight";

    private static readonly Dictionary<string, string> Canonical = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cup"] = "cup", ["cups"] = "cup", ["c"] = "cup",
        ["tablespoon"] = "tbsp", ["tablespoons"] = "tbsp", ["tbsp"] = "tbsp", ["tbs"] = "tbsp", ["tbsps"] = "tbsp",
        ["teaspoon"] = "tsp", ["teaspoons"] = "tsp", ["tsp"] = "tsp", ["tsps"] = "tsp",
        ["fl oz"] = "fl oz", ["fluid ounce"] = "fl oz", ["fluid ounces"] = "fl oz",
        ["pound"] = "lb", ["pounds"] = "lb", ["lb"] = "lb", ["lbs"] = "lb",
        ["ounce"] = "oz", ["ounces"] = "oz", ["oz"] = "oz",
        ["gram"] = "g", ["grams"] = "g", ["g"] = "g",
        ["kilogram"] = "kg", ["kilograms"] = "kg", ["kg"] = "kg",
        ["milliliter"] = "ml", ["milliliters"] = "ml", ["millilitre"] = "ml", ["millilitres"] = "ml", ["ml"] = "ml",
        ["liter"] = "l", ["liters"] = "l", ["litre"] = "l", ["litres"] = "l", ["l"] = "l",
        ["quart"] = "quart", ["quarts"] = "quart", ["qt"] = "quart", ["pint"] = "pint", ["pints"] = "pint", ["pt"] = "pint",
        ["gallon"] = "gallon", ["gallons"] = "gallon", ["gal"] = "gallon",
        ["can"] = "can", ["cans"] = "can", ["jar"] = "jar", ["jars"] = "jar",
        ["package"] = "package", ["packages"] = "package", ["pkg"] = "package",
        ["bunch"] = "bunch", ["bunches"] = "bunch", ["head"] = "head", ["heads"] = "head",
        ["clove"] = "clove", ["cloves"] = "clove", ["stalk"] = "stalk", ["stalks"] = "stalk",
        ["sprig"] = "sprig", ["sprigs"] = "sprig", ["slice"] = "slice", ["slices"] = "slice",
        ["piece"] = "piece", ["pieces"] = "piece", ["pinch"] = "pinch", ["dash"] = "dash", ["dashes"] = "dash",
    };

    /// <summary>Spellings whose case matters: in recipes a capital T is a tablespoon and a small t a teaspoon.</summary>
    private static readonly Dictionary<string, string> CaseSensitive = new(StringComparer.Ordinal)
    {
        ["T"] = "tbsp", ["Tbsp"] = "tbsp", ["t"] = "tsp",
    };

    /// <summary>How many base units (teaspoons for volume, grams for weight) one of each unit holds.</summary>
    private static readonly Dictionary<string, (string Dimension, decimal Factor, bool Metric)> Measures = new(StringComparer.Ordinal)
    {
        ["tsp"] = (Volume, 1m, false),
        ["tbsp"] = (Volume, 3m, false),
        ["fl oz"] = (Volume, 6m, false),
        ["cup"] = (Volume, 48m, false),
        ["pint"] = (Volume, 96m, false),
        ["quart"] = (Volume, 192m, false),
        ["gallon"] = (Volume, 768m, false),
        ["ml"] = (Volume, 0.202884m, true),
        ["l"] = (Volume, 202.884m, true),
        ["g"] = (Weight, 1m, true),
        ["kg"] = (Weight, 1000m, true),
        ["oz"] = (Weight, 28.3495m, false),
        ["lb"] = (Weight, 453.592m, false),
    };

    /// <summary>True when <paramref name="word"/> is a unit ChefBot recognizes.</summary>
    public static bool TryParse(string word, out string unit)
    {
        var text = word.Trim().TrimEnd('.');
        if (CaseSensitive.TryGetValue(text, out var exact) || Canonical.TryGetValue(text, out exact))
        {
            unit = exact;
            return true;
        }

        unit = string.Empty;
        return false;
    }

    /// <summary>The canonical spelling, or the lower-cased text for units ChefBot doesn't know.</summary>
    public static string Normalize(string? unit)
    {
        var text = unit?.Trim() ?? string.Empty;
        return TryParse(text, out var found) ? found : text.ToLowerInvariant();
    }

    public static bool Same(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);

    /// <summary>
    /// What a unit measures, for grouping amounts that can be added: "volume", "weight", or for anything else
    /// (cans, cloves, no unit) the unit itself.
    /// </summary>
    public static string Family(string? unit)
    {
        var normalized = Normalize(unit);
        return Measures.TryGetValue(normalized, out var measure) ? measure.Dimension : normalized;
    }

    /// <summary>True when amounts in these two units can be added: the same unit, or both volumes or both weights.</summary>
    public static bool Comparable(string? left, string? right) =>
        string.Equals(Family(left), Family(right), StringComparison.Ordinal);

    /// <summary>Re-expresses <paramref name="quantity"/> in another unit. False when the units can't be compared.</summary>
    public static bool TryConvert(decimal quantity, string? from, string? to, out decimal result)
    {
        var source = Normalize(from);
        var target = Normalize(to);
        if (string.Equals(source, target, StringComparison.Ordinal))
        {
            result = quantity;
            return true;
        }

        if (Measures.TryGetValue(source, out var a) && Measures.TryGetValue(target, out var b) && a.Dimension == b.Dimension)
        {
            result = quantity * a.Factor / b.Factor;
            return true;
        }

        result = 0m;
        return false;
    }

    /// <summary>
    /// The same amount in a unit people would write, keeping to US or metric units as <paramref name="unit"/> does:
    /// 24 tbsp becomes 1.5 cup and 1500 g becomes 1.5 kg. Amounts are rounded to two decimals. Units that can't be
    /// converted are returned as they are.
    /// </summary>
    public static (decimal Quantity, string Unit) Readable(decimal quantity, string? unit)
    {
        var normalized = Normalize(unit);
        if (!Measures.TryGetValue(normalized, out var measure))
        {
            return (quantity, normalized);
        }

        var basis = quantity * measure.Factor;
        var target = (measure.Dimension, measure.Metric) switch
        {
            (Volume, true) => basis >= Measures["l"].Factor ? "l" : "ml",
            (Weight, true) => basis >= Measures["kg"].Factor ? "kg" : "g",
            (Volume, false) => basis >= Measures["cup"].Factor / 4m ? "cup" : basis >= Measures["tbsp"].Factor ? "tbsp" : "tsp",
            _ => basis >= Measures["lb"].Factor ? "lb" : "oz",
        };

        return (Math.Round(basis / Measures[target].Factor, 2, MidpointRounding.AwayFromZero), target);
    }
}
