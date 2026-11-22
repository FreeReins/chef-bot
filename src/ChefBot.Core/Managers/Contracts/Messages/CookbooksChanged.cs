using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a cookbook was made, renamed, described or removed, or recipes were put in or taken out of one.</summary>
[MessageIdentity("chefbot.cookbooks-changed.v1")]
public sealed record CookbooksChanged;
