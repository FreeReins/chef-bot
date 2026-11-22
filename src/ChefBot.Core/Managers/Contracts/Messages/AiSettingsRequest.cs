namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Every AI model ChefBot may use, and which does what. Models left out are removed.</summary>
/// <param name="Models">The models, in the order they are shown, at most ten.</param>
/// <param name="DefaultModel">The name of the model tasks use unless another is chosen. Needed when there are any models.</param>
/// <param name="Tasks">Tasks that use a model other than the default. Tasks left out use the default.</param>
public sealed record AiSettingsRequest(
    IReadOnlyList<AiModelDraft> Models,
    string? DefaultModel = null,
    IReadOnlyList<AiTaskChoice>? Tasks = null);
