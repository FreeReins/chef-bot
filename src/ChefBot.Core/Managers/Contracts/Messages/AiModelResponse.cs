namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>An AI model ChefBot can use. The API key is never sent back, only whether one is saved.</summary>
/// <param name="Id">The model's id, used to test it.</param>
/// <param name="Name">What the household calls it.</param>
/// <param name="Provider">How ChefBot talks to it.</param>
/// <param name="Model">The model's id at the provider, such as <c>claude-opus-5-5</c>.</param>
/// <param name="Endpoint">The service's address. Empty for Anthropic's own.</param>
/// <param name="HasApiKey">True when an API key is saved.</param>
public sealed record AiModelResponse(Guid Id, string Name, AiProvider Provider, string Model, string Endpoint, bool HasApiKey);
