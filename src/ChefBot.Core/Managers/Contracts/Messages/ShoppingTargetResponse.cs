namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A to-do app the shopping list can be sent to. The token is never sent back, only whether one is saved.</summary>
/// <param name="Id">The target's id, used to send to it.</param>
/// <param name="Name">What the household calls it, such as "Kitchen list".</param>
/// <param name="Kind">The kind of app.</param>
/// <param name="Url">Home Assistant's address, or the webhook's. Empty for Todoist.</param>
/// <param name="List">The Home Assistant to-do entity (<c>todo.shopping_list</c>) or Todoist project id. Empty when not needed.</param>
/// <param name="HasToken">True when an access token is saved.</param>
public sealed record ShoppingTargetResponse(Guid Id, string Name, ShoppingTargetKind Kind, string Url, string List, bool HasToken);
