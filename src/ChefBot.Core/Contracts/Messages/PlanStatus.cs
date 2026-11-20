using System.Text.Json.Serialization;

namespace ChefBot.Core.Contracts.Messages;

/// <summary>Whether a plan is still a proposal or the one being shopped and cooked from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PlanStatus>))]
public enum PlanStatus
{
    /// <summary>A proposal to review, shuffle and adjust. Nothing has been bought against it.</summary>
    Staged,

    /// <summary>The plan the household is shopping and cooking from.</summary>
    Confirmed,
}
