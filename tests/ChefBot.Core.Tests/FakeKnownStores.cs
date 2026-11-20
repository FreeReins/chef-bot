using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeKnownStores : IKnownStoreAccess
{
    public List<KnownStore> Rows { get; } =
    [
        new() { Name = "Costco", Kind = GroceryStoreKind.WarehouseClub, Items = ["chicken", "egg"] },
        new() { Name = "Farmers market", Kind = GroceryStoreKind.FarmersMarket, SeasonalProduce = true, Months = [5, 6, 7, 8, 9, 10] },
    ];

    public IReadOnlyList<KnownStore> All() => Rows;
}
