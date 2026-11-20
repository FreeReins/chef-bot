using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>An outside recipe library that can be imported: a Mealie instance.</summary>
internal interface IRecipeSourceAccess
{
    /// <summary>Reads every recipe, using the address and token saved in <paramref name="connection"/>.</summary>
    /// <exception cref="ChefBot.Core.Managers.Contracts.RecipeSourceException">The source couldn't be reached or read.</exception>
    Task<RecipeSourceResult> FetchAllAsync(MealieSettings connection, CancellationToken cancellationToken);
}
