using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.ResourceAccess;

/// <summary>The starter recipes offered by the setup wizard.</summary>
internal interface ISampleRecipeAccess
{
    IReadOnlyList<RecipeText> All();
}
