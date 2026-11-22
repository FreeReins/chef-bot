using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class HistoryProjectionTests
{
    private static async Task<(TestCore Core, PlanResponse Plan, MealResponse Stew)> CookedStewAsync()
    {
        var core = new TestCore();
        core.Clock.Now = new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);
        core.Recipes.Rows.AddRange(TestData.Library);
        var plan = await core.PlanManager().StageAsync(new StagePlanRequest(), default);
        var stew = plan.Meals.Single(meal => meal.Title == TestData.Stew.Name && !meal.IsLeftover);
        await core.PlanManager().SetMealStatusAsync(plan.Id, stew.Id, new MealStatusRequest(MealStatus.Cooked), default);
        return (core, plan, stew);
    }

    [Test]
    public async Task Recording_the_same_cook_twice_keeps_one_moment()
    {
        var (core, _, _) = await CookedStewAsync();
        var cooked = core.Messages.Published.OfType<MealCooked>().Single();
        var source = Guid.NewGuid();

        await core.RecipeManager().RecordHistoryAsync(cooked, source, default);
        await core.RecipeManager().RecordHistoryAsync(cooked, source, default);

        await Assert.That(core.History.Rows.Count(row => row.Kind == FoodMomentKind.Cooked)).IsEqualTo(1);
    }

    [Test]
    public async Task A_cook_that_arrives_after_its_undo_is_not_brought_back()
    {
        var (core, plan, stew) = await CookedStewAsync();
        var cooked = core.Messages.Published.OfType<MealCooked>().Single();
        await core.PlanManager().SetMealStatusAsync(plan.Id, stew.Id, new MealStatusRequest(MealStatus.Planned), default);
        var undone = core.Messages.Published.OfType<MealUncooked>().Single();

        await core.RecipeManager().RecordHistoryAsync(undone, Guid.NewGuid(), default);
        await core.RecipeManager().RecordHistoryAsync(cooked, Guid.NewGuid(), default);

        await Assert.That(core.History.Rows).IsEmpty();
    }

    [Test]
    public async Task The_same_recipes_added_message_records_its_moments_once()
    {
        var core = new TestCore();
        await core.RecipeManager().CreateAsync(new RecipeDraft("Lone Pie", RecipeCourse.Main, ["1 pie crust"]), default);
        var added = core.Messages.Published.OfType<RecipesAdded>().Single();
        var source = Guid.NewGuid();

        await core.RecipeManager().RecordHistoryAsync(added, source, default);
        await core.RecipeManager().RecordHistoryAsync(added, source, default);

        await Assert.That(core.History.Rows.Single().Title).IsEqualTo("Lone Pie");
        await Assert.That(core.Messages.Published.OfType<HistoryRecorded>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Cooking_writes_no_history_in_its_own_request()
    {
        var (core, _, _) = await CookedStewAsync();

        await Assert.That(core.History.Rows).IsEmpty();
        await Assert.That(core.Messages.Published.OfType<MealCooked>().Single().FavouredBy).IsEmpty();
    }
}
