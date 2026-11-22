using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class ReviewTests
{
    private static (TestCore Core, UserAccount Jamie, UserAccount Chris) Household()
    {
        var core = new TestCore();
        core.Clock.Now = new DateTimeOffset(2026, 11, 14, 18, 0, 0, TimeSpan.Zero);
        core.Recipes.Rows.AddRange(TestData.Library);
        var jamie = new UserAccount { Username = "jamie", DisplayName = "Jamie", IsAdmin = true };
        var chris = new UserAccount { Username = "chris", DisplayName = "Chris" };
        core.Accounts.Users.AddRange([jamie, chris]);
        return (core, jamie, chris);
    }

    private static RecipeReviewRequest Stars(int stars, string? notes = null) => new(stars, notes);

    [Test]
    public async Task The_rating_is_the_average_of_every_review_to_one_decimal_place()
    {
        var (core, jamie, chris) = Household();
        var recipes = core.RecipeManager();

        await Assert.That((await recipes.GetAsync(TestData.Stew.Id, default)).Rating).IsNull();

        await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(5), default);
        core.Clock.Now = core.Clock.Now.AddDays(7);
        await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(4), default);
        core.Clock.Now = core.Clock.Now.AddDays(7);
        var reviews = await recipes.AddReviewAsync(TestData.Stew.Id, chris.Id, Stars(4, "Needed salt."), default);

        await Assert.That(reviews.Rating).IsEqualTo(new RecipeRating(4.3m, 3));
        await Assert.That(string.Join(", ", reviews.Reviews.Select(review => $"{review.ReviewedBy} {review.Stars}"))).IsEqualTo("Chris 4, Jamie 4, Jamie 5");
        await Assert.That(reviews.Reviews[0].Notes).IsEqualTo("Needed salt.");

        await Assert.That((await recipes.GetAsync(TestData.Stew.Id, default)).Rating).IsEqualTo(new RecipeRating(4.3m, 3));
        var page = await recipes.SearchAsync(new RecipeSearchRequest(null, null, 1, 100), default);
        await Assert.That(page.Recipes.Single(recipe => recipe.Id == TestData.Stew.Id).Rating).IsEqualTo(new RecipeRating(4.3m, 3));
        await Assert.That(page.Recipes.Single(recipe => recipe.Id == TestData.Soup.Id).Rating).IsNull();
        var favourite = await recipes.AddFavouriteAsync(TestData.Stew.Id, chris.Id, default);
        await Assert.That(favourite.Rating).IsEqualTo(new RecipeRating(4.3m, 3));
    }

    [Test]
    public async Task Stars_must_be_from_one_to_five_and_notes_are_tidied()
    {
        var (core, jamie, _) = Household();
        var recipes = core.RecipeManager();

        await Assert.That(async () => await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(0), default)).Throws<RuleViolationException>();
        await Assert.That(async () => await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(6), default)).Throws<RuleViolationException>();
        await Assert.That(async () => await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(3, new string('a', 2001)), default))
            .Throws<RuleViolationException>();
        await Assert.That(core.Reviews.Rows).IsEmpty();

        var saved = await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(3, "   "), default);
        await Assert.That(saved.Reviews.Single().Notes).IsNull();
        saved = await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(3, "  Good.  "), default);
        await Assert.That(saved.Reviews.Select(review => review.Notes)).Contains("Good.");
    }

    [Test]
    public async Task People_edit_only_their_own_reviews()
    {
        var (core, jamie, chris) = Household();
        var recipes = core.RecipeManager();
        var id = (await recipes.AddReviewAsync(TestData.Stew.Id, chris.Id, Stars(2), default)).Reviews.Single().Id;
        await Assert.That(core.Reviews.Rows.Single().Stars).IsEqualTo(2);

        await Assert.That(async () => await recipes.UpdateReviewAsync(TestData.Stew.Id, id, jamie.Id, Stars(5), default))
            .Throws<NotAllowedException>();

        var edited = await recipes.UpdateReviewAsync(TestData.Stew.Id, id, chris.Id, Stars(4, "Better the second night."), default);
        var review = edited.Reviews.Single();
        await Assert.That(review.Stars).IsEqualTo(4);
        await Assert.That(review.Notes).IsEqualTo("Better the second night.");
        await Assert.That(review.Edited).IsTrue();
        await Assert.That(edited.Rating).IsEqualTo(new RecipeRating(4m, 1));
    }

    [Test]
    public async Task People_delete_their_own_reviews_and_an_admin_can_delete_anyones()
    {
        var (core, jamie, chris) = Household();
        var recipes = core.RecipeManager();
        var jamies = (await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(5), default)).Reviews.Single().Id;
        var chriss = (await recipes.AddReviewAsync(TestData.Stew.Id, chris.Id, Stars(1), default)).Reviews
            .Single(review => review.UserId == chris.Id).Id;

        await Assert.That(async () => await recipes.RemoveReviewAsync(TestData.Stew.Id, jamies, chris.Id, false, default))
            .Throws<NotAllowedException>();

        var left = await recipes.RemoveReviewAsync(TestData.Stew.Id, chriss, jamie.Id, true, default);
        await Assert.That(left.Rating).IsEqualTo(new RecipeRating(5m, 1));

        var none = await recipes.RemoveReviewAsync(TestData.Stew.Id, jamies, jamie.Id, false, default);
        await Assert.That(none.Rating).IsNull();
        await Assert.That(core.Reviews.Rows).IsEmpty();
    }

    [Test]
    public async Task A_review_belongs_to_its_recipe()
    {
        var (core, jamie, _) = Household();
        var recipes = core.RecipeManager();
        var id = (await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(5), default)).Reviews.Single().Id;

        await Assert.That(async () => await recipes.UpdateReviewAsync(TestData.Soup.Id, id, jamie.Id, Stars(1), default))
            .Throws<NotFoundException>();
        await Assert.That(async () => await recipes.RemoveReviewAsync(TestData.Soup.Id, Guid.NewGuid(), jamie.Id, true, default))
            .Throws<NotFoundException>();
    }

    [Test]
    public async Task An_archived_recipe_cant_be_reviewed()
    {
        var (core, jamie, _) = Household();
        var recipes = core.RecipeManager();
        await recipes.ArchiveAsync(TestData.Stew.Id, default);

        await Assert.That(async () => await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(4), default)).Throws<NotFoundException>();
        await Assert.That(core.Reviews.Rows).IsEmpty();
    }

    [Test]
    public async Task Reviews_are_told_in_the_food_history()
    {
        var (core, jamie, _) = Household();
        var recipes = core.RecipeManager();
        await recipes.AddReviewAsync(TestData.Stew.Id, jamie.Id, Stars(4, "Lovely."), default);

        var moment = (await recipes.GetHistoryAsync(new HistoryRequest(null, 0), default)).Chapters
            .SelectMany(chapter => chapter.Moments)
            .Single(moment => moment.Kind == HistoryMomentKind.Reviewed);

        await Assert.That(moment.Title).IsEqualTo(TestData.Stew.Name);
        await Assert.That(moment.RecipeId).IsEqualTo(TestData.Stew.Id);
        await Assert.That(moment.Date).IsEqualTo(new DateOnly(2026, 11, 14));
        await Assert.That(moment.Review).IsEqualTo(new HistoryReview(4, "Lovely.", "Jamie"));

        await recipes.ArchiveAsync(TestData.Stew.Id, default);
        var archived = (await recipes.GetHistoryAsync(new HistoryRequest(null, 0), default)).Chapters
            .SelectMany(chapter => chapter.Moments)
            .Single(moment => moment.Kind == HistoryMomentKind.Reviewed);
        await Assert.That(archived.RecipeId).IsNull();
        await Assert.That(archived.Title).IsEqualTo(TestData.Stew.Name);
    }
}
