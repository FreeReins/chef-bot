using System.Globalization;
using ChefBot.Core.Accounts;
using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;
using ChefBot.Core.Shopping;
using ChefBot.Core.Swaps;

namespace ChefBot.Api.Contracts;

/// <summary>Converts core objects to the shapes the API returns.</summary>
public static class Mapping
{
    public static PlanResponse ToResponse(this PlanView view)
    {
        var plan = view.Plan;
        var meals = plan.Meals
            .OrderBy(meal => meal.Date ?? DateOnly.MaxValue)
            .ThenBy(meal => meal.Slot)
            .ThenBy(meal => meal.Title, StringComparer.OrdinalIgnoreCase)
            .Select(meal => meal.ToResponse())
            .ToList();

        var shopping = plan.ShoppingItems
            .OrderBy(item => item.IsPurchased)
            .ThenBy(item => item.Store, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.ToResponse())
            .ToList();

        return new PlanResponse(plan.Id, plan.WeekStart, plan.WeekEnd, plan.Status, meals, shopping, view.Notes);
    }

    public static MealResponse ToResponse(this PlannedMeal meal) => new(
        meal.Id,
        meal.Date,
        meal.Date?.DayOfWeek.ToString(),
        meal.Slot,
        meal.Title,
        meal.RecipeId,
        meal.Status,
        meal.IsOptional,
        meal.IsLeftover,
        meal.Recipe?.IsSlowCooker ?? false,
        meal.Portions,
        meal.GetIngredients().Select(ingredient => ingredient.DisplayName).Distinct().ToList());

    public static ShoppingItemResponse ToResponse(this ShoppingItem item) => new(
        item.Id, item.DisplayName, item.Quantity, item.Unit, item.Store, item.IsOptional, item.IsPurchased, item.PurchasedOn);

    public static PantryItemResponse ToResponse(this PantryItem item, DateOnly today, PlannerOptions options, IngredientCatalog catalog) => new(
        item.Id,
        item.DisplayName,
        item.Quantity,
        item.Unit,
        item.PurchasedOn,
        item.ExpiresOn,
        item.DaysLeft(today),
        item.FreshnessOn(today, options.ExpiringSoonDays),
        catalog.Get(item.CatalogName)?.Group);

    public static RecipeResponse ToResponse(this Recipe recipe) => new(
        recipe.Id,
        recipe.Name,
        recipe.Course,
        recipe.Origin,
        recipe.IsSlowCooker,
        recipe.Servings,
        recipe.Ingredients.OrderBy(ingredient => ingredient.Position).Select(Describe).ToList());

    public static AlternativesResponse ToResponse(this MealAlternatives alternatives) =>
        new(alternatives.Meal.ToResponse(), alternatives.Recipes, alternatives.Creative);

    public static IngredientsResponse ToResponse(this IngredientOverview overview, DateOnly today, PlannerOptions options, IngredientCatalog catalog) => new(
        overview.Ingredients,
        overview.Pantry.Select(line => new PantryUseResponse(line.Item.ToResponse(today, options, catalog), line.UsedByPlan)).ToList());

    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Username, user.DisplayName, user.IsAdmin, user.CreatedAt, user.LockedUntil);

    public static SecurityResponse ToResponse(this SecuritySettings rules) =>
        new(rules.MaxFailedSignIns, rules.LockoutMinutes, rules.RequireHttps);

    /// <summary>Writes an ingredient the way it would be typed: "1.5 lb chicken thigh".</summary>
    private static string Describe(RecipeIngredient ingredient)
    {
        var amount = ingredient.Quantity is decimal quantity ? quantity.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty;
        return string.Join(' ', new[] { amount, ingredient.Unit, ingredient.DisplayName }.Where(part => part.Length > 0));
    }
}
