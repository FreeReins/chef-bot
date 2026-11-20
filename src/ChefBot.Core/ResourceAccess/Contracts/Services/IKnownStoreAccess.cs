using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>The well-known stores offered when choosing where to shop.</summary>
internal interface IKnownStoreAccess
{
    IReadOnlyList<KnownStore> All();
}
