using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Whether a planned meal is still to come, was cooked, or was skipped.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MealStatus>))]
public enum MealStatus
{
    Planned,
    Cooked,
    Skipped,
}
