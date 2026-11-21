using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A to-do app the household sends its shopping list to, with what is needed to reach it.</summary>
internal sealed class ShoppingTarget
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    public ShoppingTargetKind Kind { get; set; }

    /// <summary>Home Assistant's address or the webhook's. Empty for Todoist.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>The access token, sent as a bearer token. Kept here only; responses say whether there is one.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>The Home Assistant to-do entity or Todoist project id. Empty when not needed.</summary>
    public string List { get; set; } = string.Empty;

    /// <summary>Where it comes in the list of targets.</summary>
    public int Position { get; set; }
}
