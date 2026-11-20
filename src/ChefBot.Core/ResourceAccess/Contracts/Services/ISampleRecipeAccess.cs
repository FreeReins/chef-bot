using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>The starter recipes offered by the setup wizard.</summary>
internal interface ISampleRecipeAccess
{
    IReadOnlyList<RecipeText> All();
}
