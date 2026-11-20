namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class GroceryStoreEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsMain { get; set; }

    public int Position { get; set; }

    public List<string> Items { get; set; } = [];

    public bool SeasonalProduce { get; set; }

    public List<int> Months { get; set; } = [];
}
