using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What put a message in the inbox.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<InboxSource>))]
public enum InboxSource
{
    /// <summary>The web page, about something the person just did there.</summary>
    Page,

    /// <summary>The weekly schedule, about a plan it staged.</summary>
    Plan,
}
