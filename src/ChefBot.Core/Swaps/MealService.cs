using ChefBot.Core.Abstractions;
using ChefBot.Core.Common;
using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;

namespace ChefBot.Core.Swaps;

/// <summary>The options for replacing one planned meal.</summary>
public sealed record MealAlternatives(PlannedMeal Meal, IReadOnlyList<RecipeAlternative> Recipes, IReadOnlyList<CreativeMeal> Creative);

/// <summary>
/// Changes individual meals on a plan: suggests swaps that use the pantry, applies them,
/// and records meals as cooked or skipped. Every change updates leftovers, the shopping
/// list and the pantry in the same transaction.
/// </summary>
public sealed class MealService(
    IRecipeRepository recipes,
    IMealPlanRepository plans,
    IPantryRepository pantry,
    IUnitOfWork unitOfWork,
    AlternativeFinder finder,
    CreativeMealGenerator creative,
    PlanRefresher refresher,
    IngredientCatalog catalog,
    PlannerOptions options,
    TimeProvider clock)
{
    public async Task<MealAlternatives> GetAlternativesAsync(
        Guid planId, Guid mealId, int recipeLimit, int creativeCount, CancellationToken cancellationToken)
    {
        var plan = await plans.GetRequiredAsync(planId, cancellationToken);
        var meal = plan.GetMeal(mealId);
        var stock = await pantry.SnapshotAsync(clock, options, cancellationToken);

        var course = CourseFor(meal.Slot);
        var alreadyPlanned = plan.Meals.Where(m => m.RecipeId is not null).Select(m => m.RecipeId!.Value).ToHashSet();
        var library = await recipes.ListActiveAsync(cancellationToken);
        var candidates = library.Where(recipe => recipe.Course == course && !alreadyPlanned.Contains(recipe.Id));

        var ranked = finder.Rank(candidates, stock, plan.WeekStart.Month, recipeLimit);

        // Grain, protein and produce plates make sense for lunch and dinner only.
        IReadOnlyList<CreativeMeal> ideas = meal.Slot is MealSlot.Lunch or MealSlot.Dinner
            ? creative.Generate(stock, creativeCount, Random.Shared)
            : [];

        return new MealAlternatives(meal, ranked, ideas);
    }

    public async Task<PlanView> UseRecipeAsync(Guid planId, Guid mealId, Guid recipeId, CancellationToken cancellationToken)
    {
        var plan = await plans.GetRequiredAsync(planId, cancellationToken);
        var meal = plan.GetMeal(mealId);
        var recipe = await recipes.GetRequiredAsync(recipeId, cancellationToken);
        if (recipe.IsArchived)
        {
            throw new RuleViolationException($"{recipe.Name} has been archived and can't be planned.");
        }

        meal.UseRecipe(recipe, options.PortionsFor(meal.Slot));
        return await SaveAsync(plan, cancellationToken);
    }

    /// <summary>Replaces a meal with a free-form one, such as a creative grain, protein and vegetable idea.</summary>
    public async Task<PlanView> UseCustomAsync(
        Guid planId, Guid mealId, string title, IReadOnlyList<string> ingredientNames, CancellationToken cancellationToken)
    {
        var ingredients = (ingredientNames ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => catalog.Identify(name))
            .DistinctBy(identity => identity.Key)
            .Select(identity => new PlannedMealIngredient
            {
                Key = identity.Key,
                CatalogName = identity.CatalogName,
                DisplayName = identity.DisplayName,
            })
            .ToList();

        if (string.IsNullOrWhiteSpace(title) || ingredients.Count == 0)
        {
            throw new RuleViolationException("A custom meal needs a title and at least one ingredient.");
        }

        var plan = await plans.GetRequiredAsync(planId, cancellationToken);
        var meal = plan.GetMeal(mealId);
        meal.UseCustom(title.Trim(), ingredients, options.PortionsFor(meal.Slot));
        return await SaveAsync(plan, cancellationToken);
    }

    /// <summary>
    /// Marks a meal as planned, cooked or skipped. Cooking a meal takes its ingredients out of
    /// the pantry where amounts can be compared. Undoing "cooked" does not put them back.
    /// </summary>
    public async Task<PlanView> SetStatusAsync(Guid planId, Guid mealId, MealStatus status, CancellationToken cancellationToken)
    {
        var plan = await plans.GetRequiredAsync(planId, cancellationToken);
        var meal = plan.GetMeal(mealId);
        if (meal.Status == status)
        {
            return new PlanView(plan, []);
        }

        var items = await pantry.ListAsync(cancellationToken);
        if (status == MealStatus.Cooked && meal.Status == MealStatus.Planned)
        {
            foreach (var finished in PantryUsage.Deduct(items, MealRequirements.For([meal])))
            {
                pantry.Remove(finished);
                items.Remove(finished);
            }
        }

        meal.Status = status;
        return await SaveAsync(plan, new PantrySnapshot(items, clock.Today(), options.ExpiringSoonDays), cancellationToken);
    }

    private async Task<PlanView> SaveAsync(MealPlan plan, CancellationToken cancellationToken)
    {
        var stock = await pantry.SnapshotAsync(clock, options, cancellationToken);
        return await SaveAsync(plan, stock, cancellationToken);
    }

    private async Task<PlanView> SaveAsync(MealPlan plan, PantrySnapshot stock, CancellationToken cancellationToken)
    {
        var draft = refresher.Refresh(plan, stock);
        await unitOfWork.CommitAsync(cancellationToken);
        return new PlanView(plan, draft.Notes().ToList());
    }

    private static RecipeCourse CourseFor(MealSlot slot) => slot switch
    {
        MealSlot.Breakfast => RecipeCourse.Breakfast,
        MealSlot.Snack => RecipeCourse.Snack,
        MealSlot.Sweet => RecipeCourse.Sweet,
        _ => RecipeCourse.Main,
    };
}
