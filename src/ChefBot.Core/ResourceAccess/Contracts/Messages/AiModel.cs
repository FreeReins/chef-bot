using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>An AI model an admin set up, with what is needed to reach it.</summary>
internal sealed class AiModel
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    public AiProvider Provider { get; set; }

    /// <summary>The model's id at the provider, such as claude-opus-5-5.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>The service's address. Empty for Anthropic's own.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>The API key. Kept here only; responses say whether there is one.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>True for the model tasks use unless another is chosen. Exactly one model has it.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Where it comes in the list of models.</summary>
    public int Position { get; set; }
}
