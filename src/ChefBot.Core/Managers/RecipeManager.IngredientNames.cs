using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>Keeps saved recipes' ingredients in step with the ingredient catalog.</content>
internal sealed partial class RecipeManager
{
    public async Task<int> RefreshIngredientNamesAsync(RefreshIngredientNames command, CancellationToken cancellationToken)
    {
        var changed = 0;
        foreach (var recipe in await recipes.ListAllAsync(cancellationToken))
        {
            if (transformation.RefreshIngredientNames(recipe))
            {
                await recipes.SaveAsync(recipe, cancellationToken);
                changed++;
            }
        }

        if (changed > 0)
        {
            await messages.PublishAsync(new IngredientNamesChanged(changed));
            await messages.CommitAsync(cancellationToken);
        }

        return changed;
    }
}
