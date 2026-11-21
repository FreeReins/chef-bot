namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The kind of to-do app a shopping list can be sent to.</summary>
public enum ShoppingTargetKind
{
    /// <summary>
    /// A to-do list in Home Assistant, such as its Shopping List, Local To-do, or a list it syncs with Bring!,
    /// Todoist or others: <c>todo.add_item</c> through its API, with a long-lived access token.
    /// </summary>
    HomeAssistant,

    /// <summary>A Todoist project (the Inbox when none is named), with an API token from Todoist's settings.</summary>
    Todoist,

    /// <summary>Any address that takes the whole list as one JSON post, for an automation to handle.</summary>
    Webhook,
}
