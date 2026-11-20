using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// A well-known store from config/stores.json. The lists turn a missing or null value into an empty list: the
/// build-time JSON reader passes null for a field the file leaves out.
/// </summary>
internal sealed record KnownStore
{
    public required string Name { get; init; }

    public GroceryStoreKind Kind { get; init; } = GroceryStoreKind.Supermarket;

    /// <summary>Where it is found or what it is good for, in a few words.</summary>
    public string Description { get; init => field = value ?? string.Empty; } = string.Empty;

    /// <summary>Ingredient catalog names usually bought there.</summary>
    public IReadOnlyList<string> Items { get; init => field = value ?? []; } = [];

    public bool SeasonalProduce { get; init; }

    public IReadOnlyList<int> Months { get; init => field = value ?? []; } = [];
}
