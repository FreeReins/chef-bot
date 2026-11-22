namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>The AI models an admin set up, and the tasks sent to a model other than the default.</summary>
internal sealed class AiSettings
{
    /// <summary>The models in their saved order. Empty when none are set up.</summary>
    public List<AiModel> Models { get; set; } = [];

    /// <summary>Tasks that use a model other than the default; each names one of <see cref="Models"/>.</summary>
    public List<AiRoute> Routes { get; set; } = [];
}
