using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>Asks an AI model a question and returns its answer.</summary>
internal interface IAiAccess
{
    /// <summary>Asks <paramref name="model"/> the question and returns its answer: a JSON object in the question's schema.</summary>
    /// <param name="model">The model to ask.</param>
    /// <param name="question">What to ask.</param>
    /// <param name="cancellationToken">Stops the request.</param>
    /// <exception cref="AiUnavailableException">The model couldn't be reached, refused, or gave no JSON object.</exception>
    Task<string> AskAsync(AiModel model, AiQuestion question, CancellationToken cancellationToken);
}
