namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>Whether a notification reached the webhook.</summary>
internal sealed record Delivery(bool Delivered, string? Problem = null);
