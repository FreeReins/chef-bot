using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Suggestions for what a recipe is missing: what may be suggested, reviewing them, and the library sweep.</summary>
public class RecipeCompletenessTests
{
    /// <summary>
    /// An answer that tries everything, including replacing what the recipe already has, which must be left out: the
    /// soup has a description, and its first line has an amount.
    /// </summary>
    private const string Answer = """
        {"description":"A hearty soup.","yield":"","prepMinutes":10,"cookMinutes":25,"totalMinutes":35,"tags":["Soup","greens"],
         "instructions":["Warm the broth in a large pot.","Add the kale and simmer until tender, about 10 minutes.","Season with salt and serve hot."],
         "ingredientAmounts":[{"line":1,"text":"2 bunches kale"},{"line":3,"text":"1 tsp salt"}],
         "missingIngredients":["1 onion, diced","4 cups chicken broth"],
         "tips":"Keeps three days in the fridge."}
        """;

    private static Recipe SparseSoup()
    {
        var soup = TestData.NewRecipe("Kale Soup", RecipeCourse.Main, false, "1 bunch kale", "4 cups chicken broth", "salt");
        soup.Description = "Green and quick.";
        soup.Instructions = ["Cook it."];
        return soup;
    }

    private static (TestCore Core, Recipe Soup) WithSoup()
    {
        var core = new TestCore();
        var soup = SparseSoup();
        core.Recipes.Rows.Add(soup);
        core.Settings.Ai = new AiSettings { Models = [new AiModel { Name = "Claude", Provider = AiProvider.Anthropic, Model = "claude-opus-5-5", ApiKey = "k", IsDefault = true }] };
        core.Ai.Answers[RecipeCompleteness.SchemaName] = Answer;
        return (core, soup);
    }

    [Test]
    public async Task Gaps_name_what_a_recipe_is_missing()
    {
        var gaps = RecipeCompleteness.Gaps(SparseSoup());

        await Assert.That(string.Join("; ", gaps)).IsEqualTo("times; tags; clear steps (they are short or missing); amounts on ingredient lines 3");
    }

    [Test]
    public async Task Only_gaps_are_filled_and_nothing_the_recipe_has_is_replaced()
    {
        var changes = RecipeCompleteness.Changes(SparseSoup(), Answer);

        await Assert.That(string.Join(", ", changes.Select(change => change.Field)))
            .IsEqualTo("PrepMinutes, CookMinutes, TotalMinutes, Tags, Instructions, IngredientAmount, NewIngredient, Notes");
        var amount = changes.Single(change => change.Field == RecipeField.IngredientAmount);
        await Assert.That(amount.Line).IsEqualTo(2);
        await Assert.That(amount.Current.Single()).IsEqualTo("salt");
        await Assert.That(changes.Single(change => change.Field == RecipeField.NewIngredient).Suggested.Single()).IsEqualTo("1 onion, diced");
        await Assert.That(string.Join(", ", changes.Single(change => change.Field == RecipeField.Tags).Suggested)).IsEqualTo("soup, greens");
        await Assert.That(string.Join(", ", changes.Select(change => change.Id))).IsEqualTo("1, 2, 3, 4, 5, 6, 7, 8");
    }

    [Test]
    public async Task Asking_keeps_suggestions_without_changing_the_recipe()
    {
        var (core, soup) = WithSoup();

        var suggestion = await core.RecipeManager().SuggestDetailsAsync(soup.Id, default);

        await Assert.That(suggestion.Status).IsEqualTo(RecipeSuggestionStatus.Waiting);
        await Assert.That(suggestion.ModelName).IsEqualTo("Claude");
        await Assert.That(core.Recipes.Rows.Single().Instructions.Single()).IsEqualTo("Cook it.");
        await Assert.That(core.Messages.Published.OfType<RecipeSuggestionsChanged>().Count()).IsEqualTo(1);
        await Assert.That(core.Messages.Commits).IsEqualTo(1);
        await Assert.That(core.Ai.Asked.Single().Question.Text).Contains("3. salt");
        await Assert.That((await core.RecipeManager().GetAsync(soup.Id, default)).HasSuggestion).IsTrue();
    }

    [Test]
    public async Task Applying_makes_only_the_ticked_changes()
    {
        var (core, soup) = WithSoup();
        var suggestion = await core.RecipeManager().SuggestDetailsAsync(soup.Id, default);
        var ticked = suggestion.Changes
            .Where(change => change.Field is RecipeField.TotalMinutes or RecipeField.IngredientAmount or RecipeField.NewIngredient or RecipeField.Notes)
            .Select(change => change.Id)
            .ToList();

        var applied = await core.RecipeManager().ApplySuggestionAsync(soup.Id, new(ticked), default);

        await Assert.That(applied.TotalMinutes).IsEqualTo(35);
        await Assert.That(applied.PrepMinutes).IsNull();
        await Assert.That(string.Join(" | ", applied.Ingredients)).IsEqualTo("1 bunch kale | 4 cups chicken broth | 1 tsp salt | 1 onion, diced");
        await Assert.That(applied.Instructions.Single()).IsEqualTo("Cook it.");
        await Assert.That(applied.Notes).IsEqualTo("Keeps three days in the fridge.");
        await Assert.That(applied.Description).IsEqualTo("Green and quick.");
        await Assert.That(applied.HasSuggestion).IsFalse();
        await Assert.That(core.Suggestions.Rows.Single().Status).IsEqualTo(RecipeSuggestionStatus.Applied);
        await Assert.That(core.Messages.Published.OfType<RecipeChanged>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Suggestions_for_an_edited_recipe_are_stale_and_refused()
    {
        var (core, soup) = WithSoup();
        var suggestion = await core.RecipeManager().SuggestDetailsAsync(soup.Id, default);
        var edited = core.Recipes.Rows.Single();
        await core.RecipeManager().UpdateAsync(soup.Id, new RecipeDraft("Kale Soup", RecipeCourse.Main, ["1 bunch kale", "4 cups broth", "salt"], Description: "Green and quick.", Instructions: [.. edited.Instructions]), default);

        var shown = await core.RecipeManager().GetSuggestionAsync(soup.Id, default);
        var error = (await TestData.Caught<ConflictException>(() => core.RecipeManager().ApplySuggestionAsync(soup.Id, new([suggestion.Changes[0].Id]), default)))!;

        await Assert.That(shown.Stale).IsTrue();
        await Assert.That(error.Message).Contains("changed after");
        await Assert.That((await core.RecipeManager().GetAsync(soup.Id, default)).HasSuggestion).IsFalse();
    }

    [Test]
    public async Task Dismissing_sets_them_aside_and_nothing_to_add_is_remembered()
    {
        var (core, soup) = WithSoup();
        await core.RecipeManager().SuggestDetailsAsync(soup.Id, default);

        await core.RecipeManager().DismissSuggestionAsync(soup.Id, default);
        await Assert.That(core.Suggestions.Rows.Single().Status).IsEqualTo(RecipeSuggestionStatus.Dismissed);

        core.Ai.Answers[RecipeCompleteness.SchemaName] = """{"description":"","tags":[],"instructions":[],"ingredientAmounts":[],"missingIngredients":[],"tips":""}""";
        var nothing = await core.RecipeManager().SuggestDetailsAsync(soup.Id, default);
        await Assert.That(nothing.Status).IsEqualTo(RecipeSuggestionStatus.NothingToAdd);
        await Assert.That(nothing.Changes).IsEmpty();
    }

    [Test]
    public async Task The_sweep_queues_sparse_recipes_not_already_looked_at()
    {
        var (core, soup) = WithSoup();
        var complete = TestData.NewRecipe("Rice", RecipeCourse.Main, false, "2 cups rice", "4 cups water");
        complete.Description = "Plain rice.";
        complete.TotalMinutes = 20;
        complete.Tags = ["side"];
        complete.Instructions = ["Rinse the rice well under cold water until the water runs clear.", "Simmer it covered in the water for eighteen minutes, then rest it off the heat for five minutes before fluffing."];
        var looked = TestData.NewRecipe("Toast", RecipeCourse.Breakfast, false, "bread");
        core.Recipes.Rows.AddRange([complete, looked]);
        core.Suggestions.Rows.Add(new RecipeSuggestion { RecipeId = looked.Id, BasedOn = RecipeCompleteness.Fingerprint(looked), Status = RecipeSuggestionStatus.Dismissed });

        var status = await core.RecipeManager().GetDetailsSweepAsync(default);
        var sweep = await core.RecipeManager().StartDetailsSweepAsync(default);

        await Assert.That(status).IsEqualTo(new DetailsSweepStatusResponse(1, 0, "Claude"));
        await Assert.That(sweep).IsEqualTo(new DetailsSweepResponse(1, "Claude"));
        await Assert.That(core.Messages.Published.OfType<SuggestRecipeDetails>().Single().RecipeId).IsEqualTo(soup.Id);
        await Assert.That(core.Messages.Commits).IsEqualTo(1);
    }

    [Test]
    public async Task A_queued_recipe_is_asked_about_once()
    {
        var (core, soup) = WithSoup();

        var first = await core.RecipeManager().SuggestDetailsForSweepAsync(new(soup.Id), default);
        var again = await core.RecipeManager().SuggestDetailsForSweepAsync(new(soup.Id), default);
        var gone = await core.RecipeManager().SuggestDetailsForSweepAsync(new(Guid.NewGuid()), default);

        await Assert.That(first).IsTrue();
        await Assert.That(again).IsFalse();
        await Assert.That(gone).IsFalse();
        await Assert.That(core.Ai.Asked.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Only_recipes_with_waiting_suggestions_are_found_by_that_filter()
    {
        var (core, soup) = WithSoup();
        core.Recipes.Rows.Add(TestData.NewRecipe("Rice", RecipeCourse.Main, false, "2 cups rice"));
        await core.RecipeManager().SuggestDetailsAsync(soup.Id, default);

        var page = await core.RecipeManager().SearchAsync(new RecipeSearchRequest(null, null, 1, 25, WithSuggestions: true), default);

        await Assert.That(page.Recipes.Single().Name).IsEqualTo("Kale Soup");
        await Assert.That(page.Recipes.Single().HasSuggestion).IsTrue();
    }

    [Test]
    public async Task Archiving_a_recipe_drops_its_suggestions()
    {
        var (core, soup) = WithSoup();
        await core.RecipeManager().SuggestDetailsAsync(soup.Id, default);

        await core.RecipeManager().ArchiveAsync(soup.Id, default);

        await Assert.That(core.Suggestions.Rows).IsEmpty();
    }
}
