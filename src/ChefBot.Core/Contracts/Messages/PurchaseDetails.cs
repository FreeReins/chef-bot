namespace ChefBot.Core.Contracts.Messages;

/// <summary>Details recorded when something is bought. Anything left null is filled in.</summary>
/// <param name="PurchasedOn">Defaults to today.</param>
/// <param name="ExpiresOn">The date on the package. Defaults to an estimate from the ingredient's shelf life.</param>
/// <param name="Quantity">What was actually bought, when it differs from the list.</param>
public sealed record PurchaseDetails(DateOnly? PurchasedOn = null, DateOnly? ExpiresOn = null, decimal? Quantity = null);
