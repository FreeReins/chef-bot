namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The AI models ChefBot may use, and which does each task.</summary>
/// <param name="Models">The models, in the order they are shown.</param>
/// <param name="DefaultModel">The name of the model tasks use unless another is chosen; null when there are none.</param>
/// <param name="Tasks">Every task, with the model chosen for it.</param>
public sealed record AiSettingsResponse(IReadOnlyList<AiModelResponse> Models, string? DefaultModel, IReadOnlyList<AiTaskResponse> Tasks);
