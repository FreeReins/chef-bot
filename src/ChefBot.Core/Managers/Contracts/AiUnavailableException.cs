namespace ChefBot.Core.Managers.Contracts;

/// <summary>An AI model couldn't be reached, refused, or gave no usable answer.</summary>
/// <param name="message">What went wrong and what to do about it, for the person who asked.</param>
/// <param name="isTransient">
/// True when trying again later may work: the service was unreachable, busy (HTTP 429) or failing (HTTP 5xx). False
/// for a wrong key, an unknown model, a refusal or an answer ChefBot couldn't read.
/// </param>
/// <param name="innerException">The error underneath, if any.</param>
public sealed class AiUnavailableException(string message, bool isTransient, Exception? innerException = null)
    : ChefBotException(message, innerException)
{
    /// <summary>True when trying again later may work.</summary>
    public bool IsTransient { get; } = isTransient;
}
