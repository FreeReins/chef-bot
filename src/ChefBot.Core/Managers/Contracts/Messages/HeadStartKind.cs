using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The sorts of thing a cook starts before the step that needs them.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<HeadStartKind>))]
public enum HeadStartKind
{
    /// <summary>Heating the oven, grill or broiler, which takes a while; start it when you start cooking.</summary>
    Preheat,

    /// <summary>Bringing a pot of water to the boil; start it early.</summary>
    Boil,

    /// <summary>A long wait, such as marinating, chilling or letting dough rise; allow time for it before you begin.</summary>
    Wait,
}
