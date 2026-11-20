namespace ChefBot.Core.Contracts.Messages;

/// <summary>Whether a notification reached the webhook.</summary>
public sealed record DeliveryResult(bool Delivered, string? Problem = null);
