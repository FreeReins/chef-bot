using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>The recipes a Mealie import adds or changes, and the counts reported back.</summary>
internal sealed record ImportChanges(IReadOnlyList<Recipe> Changed, ImportResult Result);
