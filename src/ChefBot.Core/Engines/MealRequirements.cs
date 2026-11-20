using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <summary>Works out what a set of meals needs. Shared by the shopping list, the ingredients view, cooking and reminders.</summary>
internal static class MealRequirements
{
    /// <summary>Ingredients for the meals still to be cooked. Leftovers need nothing: their dinner covers them.</summary>
    public static IReadOnlyList<RequiredIngredient> For(IEnumerable<PlannedMeal> meals)
    {
        var toCook = meals.Where(meal => meal.Status == MealStatus.Planned && !meal.IsLeftover).ToList();
        var required = new List<RequiredIngredient>();

        // A recipe planned more than once is scaled once, from its total portions.
        foreach (var batch in toCook.Where(meal => meal.Recipe is not null).GroupBy(meal => meal.Recipe!.Id))
        {
            var recipe = batch.First().Recipe!;
            var scale = Scale(batch.ToList(), recipe.Servings);
            var optional = batch.All(meal => meal.IsOptional);
            required.AddRange(recipe.Ingredients
                .OrderBy(ingredient => ingredient.Position)
                .Select(ingredient => new RequiredIngredient(ingredient, ingredient.Quantity * scale, ingredient.Unit, optional, recipe.Name)));
        }

        foreach (var meal in toCook.Where(m => m.Recipe is null))
        {
            required.AddRange(meal.CustomIngredients
                .Select(ingredient => new RequiredIngredient(ingredient, null, string.Empty, meal.IsOptional, meal.Title)));
        }

        return required;
    }

    /// <summary>
    /// How many times to multiply a recipe, rounded up to the nearest half batch.
    /// Meals with zero portions count as one batch each.
    /// </summary>
    public static decimal Scale(IReadOnlyCollection<PlannedMeal> meals, decimal servings)
    {
        var portions = meals.Sum(meal => meal.Portions);
        if (portions <= 0)
        {
            return meals.Count;
        }

        return Math.Ceiling(portions / Math.Max(servings, 1m) * 2m) / 2m;
    }
}
