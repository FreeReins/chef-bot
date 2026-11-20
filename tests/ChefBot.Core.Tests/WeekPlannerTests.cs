using ChefBot.Core.Common;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;

namespace ChefBot.Core.Tests;

public class WeekPlannerTests
{
    [Test]
    public async Task Plans_four_different_dinners_on_the_cooking_days()
    {
        var dinners = TestData.Dinners(TestData.PlanWeek(TestData.Sunday).Plan);
        var days = string.Join(",", dinners.Select(dinner => dinner.Date!.Value.DayOfWeek));

        await Assert.That(days).IsEqualTo("Sunday,Tuesday,Thursday,Saturday");
        await Assert.That(dinners.Select(dinner => dinner.RecipeId).Distinct().Count()).IsEqualTo(4);
    }

    [Test]
    public async Task Puts_the_slow_cooker_meal_on_its_day()
    {
        var dinners = TestData.Dinners(TestData.PlanWeek(TestData.Sunday).Plan);
        var tuesday = dinners.Single(dinner => dinner.Date!.Value.DayOfWeek == DayOfWeek.Tuesday);

        await Assert.That(tuesday.Title).IsEqualTo(TestData.Stew.Name);
    }

    [Test]
    public async Task Every_weekday_lunch_is_leftovers_and_dinners_are_sized_for_them()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        var lunches = plan.Meals.Where(meal => meal.Slot == MealSlot.Lunch).ToList();
        var sunday = TestData.Dinners(plan)[0];

        await Assert.That(lunches.Count).IsEqualTo(5);
        await Assert.That(lunches.All(lunch => lunch.IsLeftover)).IsTrue();

        // Sunday's dinner feeds two people that night and Monday's and Tuesday's lunches.
        await Assert.That(sunday.Portions).IsEqualTo(TestData.Options.DinnerPortions + 2 * TestData.Options.LunchPortions);
    }

    [Test]
    public async Task Adds_optional_breakfasts_snacks_and_sweets()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        var breakfasts = plan.Meals.Where(meal => meal.Slot == MealSlot.Breakfast).ToList();
        var extras = plan.Meals.Where(meal => meal.Slot is MealSlot.Snack or MealSlot.Sweet).ToList();

        await Assert.That(breakfasts.Count).IsEqualTo(5);
        await Assert.That(breakfasts.All(breakfast => breakfast.IsOptional)).IsTrue();
        await Assert.That(breakfasts.Select(breakfast => breakfast.RecipeId).Distinct().Count()).IsEqualTo(2);
        await Assert.That(extras.Count(meal => meal.Slot == MealSlot.Snack)).IsEqualTo(3);
        await Assert.That(extras.Count(meal => meal.Slot == MealSlot.Sweet)).IsEqualTo(1);
        await Assert.That(extras.All(meal => meal.Date is null)).IsTrue();
    }

    [Test]
    public async Task Prefers_meals_that_share_perishables_and_are_in_season()
    {
        var titles = TestData.Dinners(TestData.PlanWeek(TestData.Sunday).Plan).Select(dinner => dinner.Title).ToList();

        // The two cabbage-and-cilantro meals use up each other's leftovers.
        await Assert.That(titles.Contains(TestData.Tacos.Name)).IsTrue();
        await Assert.That(titles.Contains(TestData.Bowls.Name)).IsTrue();

        // Tomatoes and basil are out of season in November.
        await Assert.That(titles.Contains(TestData.Caprese.Name)).IsFalse();
        await Assert.That(titles.Contains(TestData.TomatoSalad.Name)).IsFalse();
    }

    [Test]
    public async Task Season_changes_the_choice()
    {
        var titles = TestData.Dinners(TestData.PlanWeek(TestData.SummerSunday).Plan).Select(dinner => dinner.Title).ToList();

        await Assert.That(titles.Contains(TestData.Caprese.Name) || titles.Contains(TestData.TomatoSalad.Name)).IsTrue();
    }

    [Test]
    public async Task Food_about_to_expire_pulls_its_recipe_into_the_plan()
    {
        // Tomatoes are out of season, but there are some in the pantry that expire tomorrow.
        var pantry = TestData.PantryOf(TestData.Stocked("tomatoes", 6, expiresOn: TestData.Sunday.AddDays(1)));
        var titles = TestData.Dinners(TestData.PlanWeek(TestData.Sunday, pantry).Plan).Select(dinner => dinner.Title).ToList();

        await Assert.That(titles.Contains(TestData.Caprese.Name) || titles.Contains(TestData.TomatoSalad.Name)).IsTrue();
    }

    [Test]
    public async Task Avoids_recipes_planned_recently()
    {
        var recent = new HashSet<Guid> { TestData.Tacos.Id };
        var draft = new WeekPlanner(TestData.Catalog, TestData.Options).Plan(new PlanRequest(
            TestData.Sunday, TestData.Library, recent, Pantry.PantrySnapshot.Empty, DateTimeOffset.UnixEpoch));

        await Assert.That(TestData.Dinners(draft.Plan).Any(dinner => dinner.RecipeId == TestData.Tacos.Id)).IsFalse();
    }

    [Test]
    public async Task Warns_instead_of_failing_when_there_is_no_slow_cooker_recipe()
    {
        var withoutStew = TestData.Library.Where(recipe => !recipe.IsSlowCooker).ToList();
        var draft = TestData.PlanWeek(TestData.Sunday, recipes: withoutStew);

        await Assert.That(TestData.Dinners(draft.Plan).Count).IsEqualTo(4);
        await Assert.That(draft.Warnings.Any(warning => warning.Contains("slow cooker"))).IsTrue();
    }

    [Test]
    public async Task Refuses_to_plan_with_too_few_recipes()
    {
        var onlyOne = new List<Recipe> { TestData.Tacos };
        var refused = false;
        try
        {
            TestData.PlanWeek(TestData.Sunday, recipes: onlyOne);
        }
        catch (RuleViolationException)
        {
            refused = true;
        }

        await Assert.That(refused).IsTrue();
    }

    [Test]
    public async Task Swapping_a_dinner_moves_the_leftover_lunches_with_it()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        var sunday = TestData.Dinners(plan)[0];
        var replacement = TestData.Library.First(recipe => recipe.Course == RecipeCourse.Main && plan.Meals.All(meal => meal.RecipeId != recipe.Id));

        sunday.UseRecipe(replacement, TestData.Options.DinnerPortions);
        plan.RefreshLeftoverLunches(TestData.Options);

        var monday = plan.Meals.Single(meal => meal.Slot == MealSlot.Lunch && meal.Date == TestData.Sunday.AddDays(1));
        await Assert.That(monday.RecipeId == replacement.Id).IsTrue();
        await Assert.That(monday.LeftoverOfMealId == sunday.Id).IsTrue();
        await Assert.That(plan.Meals.Count(meal => meal.Slot == MealSlot.Lunch)).IsEqualTo(5);
    }

    [Test]
    public async Task A_skipped_dinner_leaves_no_leftovers()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        var thursday = TestData.Dinners(plan)[2];

        thursday.Status = MealStatus.Skipped;
        plan.RefreshLeftoverLunches(TestData.Options);

        // Friday's lunch falls back to Tuesday's dinner, which is still within three days.
        var friday = plan.Meals.Single(meal => meal.Slot == MealSlot.Lunch && meal.Date == TestData.Sunday.AddDays(5));
        await Assert.That(friday.LeftoverOfMealId == TestData.Dinners(plan)[1].Id).IsTrue();
    }

    [Test]
    public async Task The_weekly_schedule_fires_on_the_next_matching_day_and_time()
    {
        var schedule = new WeeklyScheduleOptions { Day = DayOfWeek.Saturday, Time = "09:00" };
        var offset = TimeSpan.FromHours(-6);
        var saturdayNine = new DateTimeOffset(2026, 11, 7, 9, 0, 0, offset);

        var before = schedule.NextOccurrence(new DateTimeOffset(2026, 11, 7, 8, 0, 0, offset));
        var after = schedule.NextOccurrence(new DateTimeOffset(2026, 11, 7, 10, 0, 0, offset));
        var midweek = schedule.NextOccurrence(new DateTimeOffset(2026, 11, 4, 12, 0, 0, offset));

        await Assert.That(before == saturdayNine).IsTrue();
        await Assert.That(after == saturdayNine.AddDays(7)).IsTrue();
        await Assert.That(midweek == saturdayNine).IsTrue();
    }
}
