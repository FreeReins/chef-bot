using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How ChefBot talks to an AI model.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AiProvider>))]
public enum AiProvider
{
    /// <summary>Claude, through Anthropic's API, with an API key from the Claude Console.</summary>
    Anthropic,

    /// <summary>
    /// Any service that answers OpenAI's chat completions API: OpenAI itself, Ollama, LM Studio, OpenRouter, vLLM and
    /// others. It needs the address that ends in /v1, and an API key when the service asks for one.
    /// </summary>
    OpenAiCompatible,
}
