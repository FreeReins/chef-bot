using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Command, at startup: look saved recipes' ingredients up in the ingredient catalog again, so a change to the catalog
/// reaches recipes saved before it.
/// </summary>
[MessageIdentity("chefbot.refresh-ingredient-names.v1")]
public sealed record RefreshIngredientNames;
