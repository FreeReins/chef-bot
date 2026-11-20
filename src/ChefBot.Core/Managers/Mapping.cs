using System.Globalization;
using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <summary>Turns the internal data contracts and engine results into the public response messages, and back.</summary>
internal static class Mapping
{
    public static PlanResponse ToResponse(this MealPlan plan, IEnumerable<string> notes)
    {
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
            .Select(item => new ShoppingItemResponse(
                item.Id, item.DisplayName, item.Quantity, item.Unit, item.Store, item.IsOptional, item.IsPurchased, item.PurchasedOn))
            .ToList();

        return new PlanResponse(plan.Id, plan.WeekStart, WeekLayout.WeekEnd(plan.WeekStart), plan.Status, meals, shopping, notes.ToList());
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
        meal.Ingredients.Select(ingredient => ingredient.DisplayName).Distinct().ToList());

    public static AlternativesResponse ToResponse(this Suggestions suggestions, PlannedMeal meal) => new(
        meal.ToResponse(),
        suggestions.Recipes.Select(alternative => new RecipeAlternative(
            alternative.Recipe.Id,
            alternative.Recipe.Name,
            alternative.Recipe.IsSlowCooker,
            alternative.Score,
            alternative.OnHand,
            alternative.ExpiringSoon,
            alternative.ToBuy)).ToList(),
        suggestions.Ideas.Select(idea => new CreativeMeal(
            idea.Title,
            idea.Parts.Select(part => new CreativeComponent(part.Name, part.Group, part.OnHand, part.ExpiresOn)).ToList())).ToList());

    public static IngredientsResponse ToResponse(this IngredientOverview overview, DateOnly today, int expiringSoonDays, IngredientCatalog catalog) => new(
        overview.Ingredients.Select(line => new IngredientLine(
            line.Name, line.Status, line.Quantity, line.Unit, line.Store, line.Group, line.ExpiresOn, line.IsOptional, line.UsedIn, line.ShoppingItemId)).ToList(),
        overview.Pantry.Select(line => new PantryUseResponse(
            line.Item.ToResponse(today, expiringSoonDays, catalog),
            line.UsedByPlan,
            line.UsedFor.Select(meal => new PantryMealUse(meal.Id, meal.Title, meal.Date, meal.Slot)).ToList())).ToList());

    public static PantryItemResponse ToResponse(this PantryItem item, DateOnly today, int expiringSoonDays, IngredientCatalog catalog) => new(
        item.Id,
        item.DisplayName,
        item.Quantity,
        item.Unit,
        item.PurchasedOn,
        item.ExpiresOn,
        item.DaysLeft(today),
        item.FreshnessOn(today, expiringSoonDays),
        catalog.Get(item.CatalogName)?.Group);

    public static RecipeResponse ToResponse(this Recipe recipe)
    {
        var ingredients = recipe.Ingredients.OrderBy(ingredient => ingredient.Position).ToList();
        return new(
            recipe.Id,
            recipe.Name,
            recipe.Course,
            recipe.Origin,
            recipe.IsSlowCooker,
            recipe.Servings,
            ingredients.Select(Written).ToList(),
            recipe.Description,
            [.. recipe.Instructions],
            recipe.PrepMinutes,
            recipe.CookMinutes,
            recipe.TotalMinutes,
            recipe.SourceUrl,
            recipe.Author,
            recipe.YieldText,
            recipe.Notes,
            [.. recipe.Tags],
            recipe.Images
                .Where(image => image.IsStored)
                .OrderBy(image => image.Position)
                .Select(image => new RecipeImageResponse(image.Id, $"/api/recipes/{recipe.Id}/images/{image.Id}"))
                .ToList(),
            ingredients
                .Select(ingredient => new RecipeIngredientResponse(
                    Written(ingredient), ingredient.Quantity, ingredient.Unit, ingredient.DisplayName, ingredient.Note))
                .ToList());
    }

    public static RecipeText ToText(this RecipeDraft draft) => new(
        draft.Name,
        draft.Course,
        draft.Ingredients ?? [],
        draft.IsSlowCooker,
        draft.Servings,
        draft.Description,
        draft.Instructions,
        draft.PrepMinutes,
        draft.CookMinutes,
        draft.TotalMinutes,
        draft.SourceUrl,
        draft.Author,
        draft.YieldText,
        draft.Notes,
        draft.Tags);

    public static MealieSettingsResponse ToResponse(this MealieSettings settings) =>
        new(settings.Url, settings.ApiToken.Length > 0, settings.IsConfigured);

    public static UserResponse ToResponse(this UserAccount user) =>
        new(user.Id, user.Username, user.DisplayName, user.IsAdmin, user.CreatedAt, user.LockedUntil);

    public static SessionInfo ToSession(this UserAccount user) => new(user.Id, user.Username, user.IsAdmin, user.SessionVersion);

    public static AccountSession ToAccountSession(this UserAccount user) => new(user.ToResponse(), user.ToSession());

    public static SecurityResponse ToResponse(this SecuritySettings rules) =>
        new(rules.MaxFailedSignIns, rules.LockoutMinutes, rules.RequireHttps);

    public static NotificationSettingsResponse ToResponse(this NotificationSettings settings) => new(
        settings.WebhookUrl,
        settings.PlanReady,
        settings.SlowCooker,
        settings.MealsToday,
        settings.ExpiringFood,
        settings.Shopping,
        settings.DailyTime,
        settings.LastDailyRunOn);

    public static GroceryStoreDetails ToResponse(this GroceryStore store) =>
        new(store.Name, store.IsMain, [.. store.Items], store.SeasonalProduce, [.. store.Months]);

    public static KnownGroceryStore ToResponse(this KnownStore store) =>
        new(store.Name, store.Kind, store.Description, store.Items, store.SeasonalProduce, store.Months);

    /// <summary>
    /// The stores to save for a checked request, in its order. A store keeps its id when one with the same name is
    /// already saved. Item names are tidied the way ingredient names are, and months only count for seasonal stores.
    /// </summary>
    public static List<GroceryStore> ToStores(this IReadOnlyList<GroceryStoreDetails> details, IReadOnlyList<GroceryStore> current) =>
        details.Select((store, index) =>
        {
            var name = store.Name.Trim();
            return new GroceryStore
            {
                Id = current.FirstOrDefault(saved => string.Equals(saved.Name, name, StringComparison.OrdinalIgnoreCase))?.Id ?? Guid.CreateVersion7(),
                Name = name,
                IsMain = store.IsMain,
                Position = index,
                Items = (store.Items ?? []).Select(TextNormalizer.Normalize).Where(item => item.Length > 0).Distinct().ToList(),
                SeasonalProduce = store.SeasonalProduce,
                Months = store.SeasonalProduce ? (store.Months ?? []).Distinct().Order().ToList() : [],
            };
        }).ToList();

    public static BackupFile ToResponse(this StoredBackup file) => new(file.Name, file.CreatedAt, file.SizeBytes);

    public static BackupSummary ToResponse(this BackupCounts counts) => new(
        counts.Recipes, counts.RecipePhotos, counts.MealPlans, counts.PlannedMeals,
        counts.ShoppingItems, counts.PantryItems, counts.Users, counts.GroceryStores);

    public static DeliveryResult ToResponse(this Delivery delivery) => new(delivery.Delivered, delivery.Problem);

    /// <summary>The ingredient line as written, or rebuilt from its parts for rows saved before lines were kept.</summary>
    private static string Written(RecipeIngredient ingredient) =>
        ingredient.Text.Length > 0 ? ingredient.Text : Describe(ingredient);

    /// <summary>Writes an ingredient the way it would be typed: "1.5 lb chicken thigh".</summary>
    private static string Describe(RecipeIngredient ingredient)
    {
        var amount = ingredient.Quantity is decimal quantity ? quantity.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty;
        return string.Join(' ', new[] { amount, ingredient.Unit, ingredient.DisplayName }.Where(part => part.Length > 0));
    }
}
