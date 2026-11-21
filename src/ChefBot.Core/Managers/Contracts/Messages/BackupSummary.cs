namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What a backup holds: how many of each kind of record a restore would bring back.</summary>
public sealed record BackupSummary(
    int Recipes,
    int RecipePhotos,
    int MealPlans,
    int PlannedMeals,
    int ShoppingItems,
    int PantryItems,
    int ManualShoppingItems,
    int Users,
    int GroceryStores);
