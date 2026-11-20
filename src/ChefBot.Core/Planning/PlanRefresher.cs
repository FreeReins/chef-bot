using ChefBot.Core.Pantry;
using ChefBot.Core.Shopping;

namespace ChefBot.Core.Planning;

/// <summary>
/// Brings a plan back in line after any change: leftover lunches follow the dinners,
/// and the shopping list follows the meals and the pantry.
/// </summary>
public sealed class PlanRefresher(ShoppingListBuilder shopping, PlannerOptions options)
{
    public ShoppingDraft Refresh(MealPlan plan, PantrySnapshot pantry)
    {
        plan.RefreshLeftoverLunches(options);
        var draft = shopping.Build(plan, pantry);
        plan.ReplaceOpenShoppingItems(draft.Items);
        return draft;
    }
}
