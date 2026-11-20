using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Tests;

internal sealed class NoSamples : ISampleRecipeAccess
{
    public IReadOnlyList<RecipeText> All() => [];
}
