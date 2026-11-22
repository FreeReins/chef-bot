namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>One question for an AI model, answered with JSON in the shape of <see cref="JsonSchema"/>.</summary>
/// <param name="Instructions">What the model is to do, sent as the system prompt.</param>
/// <param name="Text">The question itself, after any images.</param>
/// <param name="Images">Images to look at, in order. Empty for a text-only question.</param>
/// <param name="JsonSchema">The JSON schema the answer must follow: an object whose properties are all required.</param>
/// <param name="SchemaName">A short name for the schema, such as recipe, which OpenAI-compatible services ask for.</param>
internal sealed record AiQuestion(string Instructions, string Text, IReadOnlyList<AiImage> Images, string JsonSchema, string SchemaName);
