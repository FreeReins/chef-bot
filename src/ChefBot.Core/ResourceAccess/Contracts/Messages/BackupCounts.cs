namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>How many of each kind of record a backup file holds.</summary>
internal sealed record BackupCounts(
    int Recipes,
    int RecipePhotos,
    int MealPlans,
    int PlannedMeals,
    int ShoppingItems,
    int PantryItems,
    int ManualShoppingItems,
    int Users,
    int GroceryStores,
    int PlanStyles);
