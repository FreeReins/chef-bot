namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>An AI model ChefBot can use, as set up in the settings.</summary>
/// <param name="Id">The saved model this edits, or null for a new one.</param>
/// <param name="Name">What the household calls it, such as "Claude" or "Ollama in the basement". Tasks name models by this.</param>
/// <param name="Provider">How ChefBot talks to it.</param>
/// <param name="Model">The model's id at the provider, such as <c>claude-opus-5-5</c> or <c>qwen2.5vl</c>.</param>
/// <param name="Endpoint">
/// The service's address. Needed for an OpenAI-compatible service, such as <c>http://ollama.local:11434/v1</c>;
/// for Anthropic, empty uses Anthropic's own.
/// </param>
/// <param name="ApiKey">A new API key. Null keeps the saved one; an empty text removes it.</param>
public sealed record AiModelDraft(
    Guid? Id,
    string Name,
    AiProvider Provider,
    string Model,
    string? Endpoint = null,
    string? ApiKey = null);
