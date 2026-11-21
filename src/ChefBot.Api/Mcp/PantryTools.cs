using System.ComponentModel;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using ModelContextProtocol.Server;

namespace ChefBot.Api.Mcp;

/// <summary>MCP tools for what ChefBot thinks is in the kitchen. Changes rebuild the open plans' shopping lists.</summary>
[McpServerToolType]
public sealed class PantryTools(IPantryManager pantry)
{
    [McpServerTool(Name = "list_pantry", ReadOnly = true, Idempotent = true)]
    [Description("Everything in the pantry with its amount and expiration date, soonest to expire first.")]
    public Task<IReadOnlyList<PantryItemResponse>> ListPantry(CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => pantry.ListAsync(cancellationToken));

    [McpServerTool(Name = "add_pantry_item")]
    [Description("Adds something to the pantry by hand, for example food bought outside a plan.")]
    public Task<PantryItemResponse> AddPantryItem(
        [Description("What it is, such as \"eggs\".")] string name,
        [Description("How much there is.")] decimal? quantity = null,
        [Description("The unit of the quantity.")] string? unit = null,
        [Description("When it was bought (yyyy-MM-dd). Defaults to today.")] DateOnly? purchasedOn = null,
        [Description("When it expires (yyyy-MM-dd). Leave empty to estimate it from the kind of food.")] DateOnly? expiresOn = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => pantry.AddAsync(new NewPantryItem(name, quantity, unit, purchasedOn, expiresOn), cancellationToken));

    [McpServerTool(Name = "update_pantry_item", Idempotent = true)]
    [Description("Corrects the amount left or the expiration date of a pantry item. Leave a value empty to keep it.")]
    public Task<PantryItemResponse> UpdatePantryItem(
        [Description("The item's id, from list_pantry.")] Guid itemId,
        [Description("The amount left.")] decimal? quantity = null,
        [Description("The new expiration date (yyyy-MM-dd).")] DateOnly? expiresOn = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => pantry.UpdateAsync(itemId, new UpdatePantryItemRequest(quantity, expiresOn), cancellationToken));

    [McpServerTool(Name = "remove_pantry_item", Destructive = true, Idempotent = true)]
    [Description("Takes something out of the pantry, for example when it was used up or thrown away.")]
    public Task<string> RemovePantryItem(
        [Description("The item's id, from list_pantry.")] Guid itemId,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => pantry.RemoveAsync(itemId, cancellationToken), "Removed from the pantry.");
}
