using ChefBot.Core.Abstractions;
using ChefBot.Core.Ingredients;

namespace ChefBot.Core.Recipes;

/// <summary>Turns ingredient text, typed in or imported, into stored recipe ingredients.</summary>
public sealed class RecipeIngredientBuilder(IngredientCatalog catalog)
{
    /// <summary>Builds ingredients from free-text lines such as "1 lb chicken thighs". Blank lines are skipped.</summary>
    public List<RecipeIngredient> FromLines(IEnumerable<string> lines) =>
        Number(lines.Where(line => !string.IsNullOrWhiteSpace(line)).Select(FromLine));

    /// <summary>Builds ingredients from an outside source, using its parsed food when it has one.</summary>
    public List<RecipeIngredient> FromSource(IEnumerable<SourceIngredient> rows) =>
        Number(rows.Select(FromSourceRow).OfType<RecipeIngredient>());

    private RecipeIngredient FromLine(string line)
    {
        var parsed = IngredientLineParser.Parse(line);
        return Create(parsed.Name, parsed.Quantity, parsed.Unit);
    }

    private RecipeIngredient? FromSourceRow(SourceIngredient row)
    {
        if (!string.IsNullOrWhiteSpace(row.FoodName))
        {
            return Create(row.FoodName, row.Quantity > 0 ? row.Quantity : null, row.Unit);
        }

        // Section headings and empty rows have no text at all.
        return string.IsNullOrWhiteSpace(row.Text) ? null : FromLine(row.Text);
    }

    private RecipeIngredient Create(string name, decimal? quantity, string? unit)
    {
        var identity = catalog.Identify(name);
        return new RecipeIngredient
        {
            Key = identity.Key,
            CatalogName = identity.CatalogName,
            DisplayName = identity.DisplayName,
            Quantity = quantity,
            Unit = Units.Normalize(unit),
        };
    }

    private static List<RecipeIngredient> Number(IEnumerable<RecipeIngredient> ingredients)
    {
        var list = ingredients.ToList();
        for (var position = 0; position < list.Count; position++)
        {
            list[position].Position = position;
        }

        return list;
    }
}
