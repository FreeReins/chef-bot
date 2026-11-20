namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Mark every open shopping item as bought. Send <c>{}</c> for today.</summary>
public sealed record PurchaseAllRequest(DateOnly? PurchasedOn = null);
