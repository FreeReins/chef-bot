using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a cookbook was removed. Its recipes stay in the library; plan style rules stop naming it.</summary>
/// <param name="CookbookId">The cookbook that was removed.</param>
[MessageIdentity("chefbot.cookbook-removed.v1")]
public sealed record CookbookRemoved(Guid CookbookId);
