using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>The recipes a Mealie import adds or changes, and the counts reported back.</summary>
internal sealed record ImportChanges(IReadOnlyList<Recipe> Changed, ImportResult Result);
