using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies MealPlanEntity to and from MealPlan.</content>
internal static partial class EntityMapping
{
    public static MealPlan ToContract(this MealPlanEntity entity)
    {
        // Meals that follow the same recipe share one copy of it, as they share one row.
        var recipes = entity.Meals
            .Where(meal => meal.Recipe is not null)
            .Select(meal => meal.Recipe!)
            .DistinctBy(recipe => recipe.Id)
            .ToDictionary(recipe => recipe.Id, recipe => recipe.ToContract());

        return new MealPlan
        {
            Id = entity.Id,
            WeekStart = entity.WeekStart,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            StyleName = entity.StyleName,
            LeftoverLunches = entity.LeftoverLunches,
            People = entity.People,
            Meals = entity.Meals.Select(meal => new PlannedMeal
            {
                Id = meal.Id,
                Date = meal.Date,
                Slot = meal.Slot,
                RecipeId = meal.RecipeId,
                Recipe = meal.RecipeId is Guid id ? recipes.GetValueOrDefault(id) : null,
                Title = meal.Title,
                Portions = meal.Portions,
                IsOptional = meal.IsOptional,
                LeftoverOfMealId = meal.LeftoverOfMealId,
                Status = meal.Status,
                CustomIngredients = meal.CustomIngredients.Select(i => new PlannedMealIngredient
                {
                    Id = i.Id,
                    Key = i.Key,
                    CatalogName = i.CatalogName,
                    DisplayName = i.DisplayName,
                }).ToList(),
            }).ToList(),
            ShoppingItems = entity.ShoppingItems.Select(item => new ShoppingItem
            {
                Id = item.Id,
                Key = item.Key,
                CatalogName = item.CatalogName,
                DisplayName = item.DisplayName,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Store = item.Store,
                IsOptional = item.IsOptional,
                IsPurchased = item.IsPurchased,
                PurchasedOn = item.PurchasedOn,
            }).ToList(),
        };
    }

    public static MealPlanEntity ToEntity(this MealPlan plan) => new()
    {
        Id = plan.Id,
        WeekStart = plan.WeekStart,
        Status = plan.Status,
        CreatedAt = plan.CreatedAt,
        StyleName = plan.StyleName,
        LeftoverLunches = plan.LeftoverLunches,
        People = plan.People,
        Meals = plan.Meals.Select(meal => new PlannedMealEntity
        {
            Id = meal.Id,
            MealPlanId = plan.Id,
            Date = meal.Date,
            Slot = meal.Slot,
            RecipeId = meal.RecipeId,
            Title = meal.Title,
            Portions = meal.Portions,
            IsOptional = meal.IsOptional,
            LeftoverOfMealId = meal.LeftoverOfMealId,
            Status = meal.Status,
            CustomIngredients = meal.CustomIngredients.Select(i => new PlannedMealIngredientEntity
            {
                Id = i.Id,
                PlannedMealId = meal.Id,
                Key = i.Key,
                CatalogName = i.CatalogName,
                DisplayName = i.DisplayName,
            }).ToList(),
        }).ToList(),
        ShoppingItems = plan.ShoppingItems.Select(item => new ShoppingItemEntity
        {
            Id = item.Id,
            MealPlanId = plan.Id,
            Key = item.Key,
            CatalogName = item.CatalogName,
            DisplayName = item.DisplayName,
            Quantity = item.Quantity,
            Unit = item.Unit,
            Store = item.Store,
            IsOptional = item.IsOptional,
            IsPurchased = item.IsPurchased,
            PurchasedOn = item.PurchasedOn,
        }).ToList(),
    };
}
