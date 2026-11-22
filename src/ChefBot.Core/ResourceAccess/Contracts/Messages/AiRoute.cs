using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A task that uses a model other than the default.</summary>
internal sealed class AiRoute
{
    public AiTask Task { get; set; }

    public Guid ModelId { get; set; }
}
