namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A to-do app to send the shopping list to, as set up in the settings.</summary>
/// <param name="Id">The saved target this edits, or null for a new one.</param>
/// <param name="Name">What the household calls it, such as "Kitchen list".</param>
/// <param name="Kind">The kind of app.</param>
/// <param name="Url">Home Assistant's address (<c>http://homeassistant.local:8123</c>) or the webhook's. Not used for Todoist.</param>
/// <param name="List">The Home Assistant to-do entity, such as <c>todo.shopping_list</c>, or a Todoist project id (optional).</param>
/// <param name="Token">A new access token. Null keeps the saved one; an empty text removes it.</param>
public sealed record ShoppingTargetDraft(
    Guid? Id,
    string Name,
    ShoppingTargetKind Kind,
    string? Url = null,
    string? List = null,
    string? Token = null);
