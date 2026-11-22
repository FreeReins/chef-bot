namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// The words that sort imported recipes into courses. They are matched as whole words against a recipe's tags,
/// categories and tools. A new instance holds the defaults.
/// </summary>
internal sealed class ClassifierWords
{
    public List<string> BreakfastWords { get; set; } = ["breakfast", "brunch"];

    public List<string> SnackWords { get; set; } = ["snack", "snacks", "appetizer", "appetizers"];

    public List<string> SweetWords { get; set; } = ["dessert", "desserts", "sweet", "sweets", "treat", "treats", "cookies", "cake"];

    public List<string> DrinkWords { get; set; } =
        ["drink", "drinks", "beverage", "beverages", "cocktail", "cocktails", "mocktail", "mocktails", "smoothie", "smoothies", "lemonade"];

    /// <summary>Also matched against the recipe name.</summary>
    public List<string> SlowCookerWords { get; set; } = ["slow cooker", "crockpot", "crock pot"];

    /// <summary>Labels for things that are never a meal on their own, such as sides and sauces.</summary>
    public List<string> NotAMealWords { get; set; } = ["side", "sides", "side dish", "sauce", "condiment", "bread", "baking"];

    /// <summary>When set, only recipes carrying one of these labels count as mains.</summary>
    public List<string> MainWords { get; set; } = [];
}
