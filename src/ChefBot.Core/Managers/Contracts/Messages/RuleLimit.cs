using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How a plan style's rule counts the meals that match it.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RuleLimit>))]
public enum RuleLimit
{
    /// <summary>At least this many meals match.</summary>
    AtLeast,

    /// <summary>No more than this many meals match.</summary>
    AtMost,

    /// <summary>Exactly this many meals match; the others don't.</summary>
    Exactly,

    /// <summary>No meal matches. The count is ignored.</summary>
    None,
}
