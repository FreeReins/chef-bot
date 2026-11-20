using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class NoSamples : ISampleRecipeAccess
{
    public IReadOnlyList<RecipeText> All() => [];
}
