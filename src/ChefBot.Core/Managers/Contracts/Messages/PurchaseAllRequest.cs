namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Mark every open shopping item as bought. Send <c>{}</c> for today. With <c>Store</c>, only the items bought at that
/// store (matched ignoring case and surrounding spaces).
/// </summary>
public sealed record PurchaseAllRequest(DateOnly? PurchasedOn = null, string? Store = null);
