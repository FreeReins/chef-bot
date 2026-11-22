using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <content>Sizing meals for the people a plan is for, and leftover lunches.</content>
internal sealed partial class PlanningEngine
{
    /// <summary>
    /// Rebuilds the weekday leftover lunches from the dinners as they stand, then sizes each
    /// dinner to cover the lunches that depend on it. Lunches that were set by hand, cooked
    /// or skipped are left alone. A plan made without leftover lunches gets none. Changes <paramref name="plan"/> in place.
    /// </summary>
    /// <param name="plan">The plan to change.</param>
    /// <param name="maxLeftoverDays">A lunch only uses a dinner cooked within this many days.</param>
    public static void PlaceLeftovers(MealPlan plan, int maxLeftoverDays)
    {
        plan.Meals.RemoveAll(meal => meal is { Slot: MealSlot.Lunch, IsLeftover: true, Status: MealStatus.Planned });

        if (plan.LeftoverLunches)
        {
            var dinners = plan.Meals
                .Where(meal => meal is { Slot: MealSlot.Dinner, Date: not null, PortionsChosen: false } && meal.Status != MealStatus.Skipped)
                .OrderByDescending(meal => meal.Date)
                .ToList();

            foreach (var day in WeekLayout.Weekdays)
            {
                var date = WeekLayout.DateFor(plan.WeekStart, day);
                if (plan.Meals.Any(meal => meal.Slot == MealSlot.Lunch && meal.Date == date))
                {
                    continue;
                }

                var earliest = date.AddDays(-maxLeftoverDays);
                var source = dinners.FirstOrDefault(dinner => dinner.Date < date && dinner.Date >= earliest);
                if (source is not null)
                {
                    plan.Meals.Add(LeftoverOf(source, date));
                }
            }
        }

        foreach (var dinner in plan.Meals.Where(meal => meal is { Slot: MealSlot.Dinner, Status: MealStatus.Planned, PortionsChosen: false }))
        {
            var lunches = plan.Meals.Count(meal => meal.LeftoverOfMealId == dinner.Id);
            dinner.Portions = plan.People * (1 + lunches);
        }
    }

    /// <summary>Makes a meal a fresh cooking of <paramref name="recipe"/>, for <paramref name="people"/>.</summary>
    public static void UseRecipe(PlannedMeal meal, Recipe recipe, int people)
    {
        meal.Recipe = recipe;
        meal.RecipeId = recipe.Id;
        meal.Title = recipe.Name;
        meal.CustomIngredients.Clear();
        ResetAsFreshMeal(meal, people);
    }

    /// <summary>Makes a meal a free-form one, such as a grain, protein and vegetable bowl, for <paramref name="people"/>.</summary>
    public static void UseCustom(PlannedMeal meal, string title, IEnumerable<PlannedMealIngredient> ingredients, int people)
    {
        meal.Recipe = null;
        meal.RecipeId = null;
        meal.Title = title;
        meal.CustomIngredients.Clear();
        meal.CustomIngredients.AddRange(ingredients);
        ResetAsFreshMeal(meal, people);
    }

    /// <summary>
    /// Plans the week for a different number of people: every meal still to be made is sized for them. Dinners that
    /// feed leftover lunches are sized again by <see cref="PlaceLeftovers"/>. Changes <paramref name="plan"/> in place.
    /// </summary>
    public static void Resize(MealPlan plan, int people)
    {
        plan.People = people;
        foreach (var meal in plan.Meals.Where(meal => meal is { IsLeftover: false, Status: MealStatus.Planned, PortionsChosen: false }))
        {
            meal.Portions = PortionsFor(meal.Slot, people);
        }
    }

    /// <summary>
    /// The portions a fresh meal is made in: one for each person at breakfast, lunch and dinner. Snacks, sweets and
    /// drinks are made once, as the recipe is written.
    /// </summary>
    public static int PortionsFor(MealSlot slot, int people) =>
        slot is MealSlot.Breakfast or MealSlot.Lunch or MealSlot.Dinner ? people : 0;

    /// <summary>
    /// Adds <paramref name="recipe"/> to the plan as a fresh meal on <paramref name="date"/> (none for any day). With
    /// <paramref name="servings"/> it is made for that many and keeps that size; without, it is sized for the plan's people.
    /// </summary>
    public static PlannedMeal AddRecipe(MealPlan plan, Recipe recipe, DateOnly? date, MealSlot slot, int? servings)
    {
        var meal = ForRecipe(recipe, slot, date, PortionsFor(slot, plan.People));
        ChoosePortions(meal, servings);
        plan.Meals.Add(meal);
        return meal;
    }

    /// <summary>Fixes the meal at <paramref name="servings"/> portions. Null leaves it sized for the plan's people.</summary>
    public static void ChoosePortions(PlannedMeal meal, int? servings)
    {
        if (servings is int chosen)
        {
            meal.Portions = chosen;
            meal.PortionsChosen = true;
        }
    }

    /// <summary>
    /// A staged plan for the week with no meals yet, in <paramref name="style"/>, for when a recipe is added before the
    /// week is planned.
    /// </summary>
    public static MealPlan EmptyPlan(DateOnly weekStart, PlanStyle style, int people, DateTimeOffset createdAt) => new()
    {
        WeekStart = weekStart,
        CreatedAt = createdAt,
        StyleName = style.Name,
        LeftoverLunches = style.LeftoverLunches,
        People = people,
    };

    private static void ResetAsFreshMeal(PlannedMeal meal, int people)
    {
        meal.Portions = PortionsFor(meal.Slot, people);
        meal.PortionsChosen = false;
        meal.LeftoverOfMealId = null;
        meal.Status = MealStatus.Planned;
    }

    private static PlannedMeal LeftoverOf(PlannedMeal dinner, DateOnly date) => new()
    {
        Slot = MealSlot.Lunch,
        Date = date,
        RecipeId = dinner.RecipeId,
        Recipe = dinner.Recipe,
        Title = $"Leftovers: {dinner.Title}",
        LeftoverOfMealId = dinner.Id,
    };
}
