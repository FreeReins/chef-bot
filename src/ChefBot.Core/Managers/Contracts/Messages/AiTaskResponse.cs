namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A task ChefBot gives an AI model, and which model does it.</summary>
/// <param name="Task">The task.</param>
/// <param name="Label">What the task is called on the page, such as "Reading recipe photos".</param>
/// <param name="Description">What the model needs to be able to do for it.</param>
/// <param name="Model">The name of the model chosen for it, or null when it uses the default model.</param>
public sealed record AiTaskResponse(AiTask Task, string Label, string Description, string? Model);
