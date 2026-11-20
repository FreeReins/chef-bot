using System.Text.Json.Serialization;

namespace ChefBot.Core.Contracts.Messages;

/// <summary>What a single-use link lets its holder do.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LinkPurpose>))]
public enum LinkPurpose
{
    /// <summary>Create a new account.</summary>
    Invite,

    /// <summary>Choose a new password for an existing account.</summary>
    PasswordReset,
}
