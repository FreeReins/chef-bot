using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Reading recipe steps for cooking mode: timers, head starts and the ingredients each step names.</summary>
public class CookingStepsTests
{
    [Test]
    [Arguments("Simmer for 20 minutes.", "20 minutes", 1200)]
    [Arguments("Bake 25 to 30 minutes, until golden.", "25 to 30 minutes", 1500)]
    [Arguments("Roast 40-45 min.", "40-45 min", 2400)]
    [Arguments("Cook on low for 8 hours.", "8 hours", 28800)]
    [Arguments("Braise for 1 hour 30 minutes.", "1 hour 30 minutes", 5400)]
    [Arguments("Braise for 1 hour and 15 minutes.", "1 hour and 15 minutes", 4500)]
    [Arguments("Stir for 30 seconds.", "30 seconds", 30)]
    [Arguments("Rest for 1 1/2 hours.", "1 1/2 hours", 5400)]
    [Arguments("Chill for 1½ hours.", "1½ hours", 5400)]
    [Arguments("Boil for a minute.", "a minute", 60)]
    [Arguments("Bake for about an hour.", "an hour", 3600)]
    [Arguments("Let it sit for a 10-minute rest.", "10-minute", 600)]
    public async Task Reads_the_time_a_step_gives(string step, string text, int seconds)
    {
        var timers = CookingSteps.Timers(step);

        await Assert.That(timers.Count).IsEqualTo(1);
        await Assert.That(timers[0].Text).IsEqualTo(text);
        await Assert.That(timers[0].Seconds).IsEqualTo(seconds);
    }

    [Test]
    public async Task Keeps_separate_times_apart()
    {
        var timers = CookingSteps.Timers("Simmer 10 minutes, then add the kale and cook 5 minutes more.");

        await Assert.That(timers.Select(timer => timer.Seconds)).IsEquivalentTo([600, 300]);
    }

    [Test]
    [Arguments("Cut into 2-inch pieces.")]
    [Arguments("Season to taste.")]
    [Arguments("Serves 4.")]
    public async Task Finds_no_timer_where_there_is_no_time(string step)
    {
        await Assert.That(CookingSteps.Timers(step)).IsEmpty();
    }

    [Test]
    [Arguments("Preheat the oven to 400°F. Line a baking sheet with foil.", HeadStartKind.Preheat, "Preheat the oven to 400°F")]
    [Arguments("Pat the chicken dry. Heat oven to 425 degrees.", HeadStartKind.Preheat, "Heat oven to 425 degrees")]
    [Arguments("Preheat the grill to medium-high.", HeadStartKind.Preheat, "Preheat the grill to medium-high")]
    [Arguments("Bring a large pot of salted water to a boil.", HeadStartKind.Boil, "Bring a large pot of salted water to a boil")]
    [Arguments("Whisk the marinade. Cover and refrigerate for at least 4 hours or overnight.", HeadStartKind.Wait, "Cover and refrigerate for at least 4 hours or overnight")]
    [Arguments("Soak the beans overnight.", HeadStartKind.Wait, "Soak the beans overnight")]
    [Arguments("Let the dough rise until doubled, about 1 hour.", HeadStartKind.Wait, "Let the dough rise until doubled, about 1 hour")]
    public async Task Finds_what_to_start_ahead(string step, HeadStartKind kind, string text)
    {
        var headStart = CookingSteps.HeadStart(step);

        await Assert.That(headStart).IsEqualTo(new RecipeHeadStartResponse(kind, text));
    }

    [Test]
    [Arguments("Preheat a skillet over medium heat.")]
    [Arguments("Let the steak rest for 5 minutes.")]
    [Arguments("Add the rest of the water and bring to a simmer.")]
    [Arguments("Bring the sauce to a boil.")]
    public async Task Leaves_short_or_immediate_work_in_its_step(string step)
    {
        await Assert.That(CookingSteps.HeadStart(step)).IsNull();
    }

    [Test]
    public async Task Names_the_ingredients_each_step_uses()
    {
        string[] ingredients = ["ground beef", "olive oil", "sesame oil", "fish fillets", "onions", "garlic"];
        string[] steps =
        [
            "Heat the olive oil and brown the beef.",
            "Add the onion and garlic; cook until soft.",
            "Season the fish and drizzle with a little oil.",
            "Finish with sesame oil.",
        ];

        var read = CookingSteps.Read(steps, ingredients);

        await Assert.That(read[0].Ingredients).IsEquivalentTo([0, 1]);
        await Assert.That(read[1].Ingredients).IsEquivalentTo([4, 5]);
        await Assert.That(read[2].Ingredients).IsEquivalentTo([3]);
        await Assert.That(read[3].Ingredients).IsEquivalentTo([2]);
    }

    [Test]
    public async Task A_saved_recipe_comes_back_with_its_steps_read()
    {
        var recipes = new TestCore().RecipeManager();

        var chili = await recipes.CreateAsync(
            new RecipeDraft("Chili", RecipeCourse.Main, ["1 lb ground beef", "1 can beans"], Instructions: ["Brown the beef.", "Add the beans and simmer 30 minutes."]),
            CancellationToken.None);

        await Assert.That(chili.StepDetails.Count).IsEqualTo(2);
        await Assert.That(chili.StepDetails[0].Text).IsEqualTo("Brown the beef.");
        await Assert.That(chili.StepDetails[0].Ingredients).IsEquivalentTo([0]);
        await Assert.That(chili.StepDetails[1].Ingredients).IsEquivalentTo([1]);
        await Assert.That(chili.StepDetails[1].Timers[0].Seconds).IsEqualTo(1800);
    }
}
