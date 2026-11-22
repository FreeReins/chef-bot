namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Which model does one task.</summary>
/// <param name="Task">The task.</param>
/// <param name="Model">The name of the model to use for it, or null to use the default model.</param>
public sealed record AiTaskChoice(AiTask Task, string? Model = null);
