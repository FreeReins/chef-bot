using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>
/// AI models that answer from <see cref="Answers"/>, keyed by the question's schema name (such as recipe_photo), and
/// keep what they were asked. Set <see cref="Failure"/> to have every question fail with it.
/// </summary>
internal sealed class FakeAi : IAiAccess
{
    public Dictionary<string, string> Answers { get; } = [];

    public List<(AiModel Model, AiQuestion Question)> Asked { get; } = [];

    public AiUnavailableException? Failure { get; set; }

    public Task<string> AskAsync(AiModel model, AiQuestion question, CancellationToken cancellationToken)
    {
        Asked.Add((model, question));
        if (Failure is not null)
        {
            throw Failure;
        }

        return Answers.TryGetValue(question.SchemaName, out var answer)
            ? Task.FromResult(answer)
            : throw new AiUnavailableException($"{model.Name} has no answer for {question.SchemaName}.", false);
    }
}
