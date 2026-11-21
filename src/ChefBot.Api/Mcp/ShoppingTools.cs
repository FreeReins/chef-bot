using System.ComponentModel;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using ModelContextProtocol.Server;

namespace ChefBot.Api.Mcp;

/// <summary>
/// MCP tools for shopping. A plan's own shopping list comes with the plan; these cover what a plan needs and the
/// things put on the list by hand.
/// </summary>
[McpServerToolType]
public sealed class ShoppingTools(IPlanManager plans)
{
    [McpServerTool(Name = "get_plan_ingredients", ReadOnly = true, Idempotent = true)]
    [Description("What a plan's meals need, where each ingredient comes from (the pantry or the shopping list), and what the pantry holds.")]
    public Task<IngredientsResponse> GetPlanIngredients(
        [Description("The plan's id.")] Guid planId,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.GetIngredientsAsync(planId, cancellationToken));

    [McpServerTool(Name = "list_shopping_items", ReadOnly = true, Idempotent = true)]
    [Description("The things put on the shopping list by hand rather than by a plan, each with the store it is bought at.")]
    public Task<IReadOnlyList<ManualShoppingItemResponse>> ListShoppingItems(CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.ListManualItemsAsync(cancellationToken));

    [McpServerTool(Name = "add_shopping_item")]
    [Description("Puts something on the shopping list by hand. Adding the same thing in the same unit again adds to its amount. Returns the whole hand-added list.")]
    public Task<IReadOnlyList<ManualShoppingItemResponse>> AddShoppingItem(
        [Description("What to buy, such as \"milk\".")] string name,
        [Description("How much, when it matters.")] decimal? quantity = null,
        [Description("The unit of the quantity, such as \"l\" or \"g\".")] string? unit = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.AddManualItemAsync(new NewShoppingItem(name, quantity, unit), cancellationToken));

    [McpServerTool(Name = "remove_shopping_item", Destructive = true, Idempotent = true)]
    [Description("Takes a hand-added item off the shopping list without buying it. Returns the whole hand-added list.")]
    public Task<IReadOnlyList<ManualShoppingItemResponse>> RemoveShoppingItem(
        [Description("The item's id, from list_shopping_items.")] Guid itemId,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.RemoveManualItemAsync(itemId, cancellationToken));
}
