using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <summary>
/// Tells the household's food history from the stored moments: picks out debuts, fan favourites, comebacks and
/// milestone dinners and someone's favourite being cooked, tells each review, folds the everyday cooking of each week into one moment and a day of many new recipes into a
/// stampede, and groups it all into monthly chapters. Reads and saves nothing.
/// </summary>
internal sealed class HistoryEngine
{
    public const int DefaultMonths = 6;

    public const int MaxMonths = 24;

    /// <summary>The days a recipe has to rest before cooking it again is a comeback.</summary>
    public const int ComebackDays = 90;

    /// <summary>How many recipes added on one day make a stampede instead of a moment each.</summary>
    public const int StampedeSize = 5;

    /// <summary>How many of a stampede's recipes are named.</summary>
    private const int StampedeNames = 6;

    private static readonly int[] FanFavouriteCooks = [5, 10, 25, 50, 100];

    private static readonly int[] DinnerMilestones = [50, 100, 250, 500, 1000];

    /// <summary>The chapters <paramref name="request"/> asks for.</summary>
    /// <param name="moments">Every stored moment, in any order.</param>
    /// <param name="reviews">Every review of a recipe, in any order.</param>
    /// <param name="recipes">Every recipe, archived ones included, for links and tags.</param>
    /// <param name="request">Which months to tell.</param>
    /// <param name="zone">The household's time zone, which the stored days are in.</param>
    public HistoryResponse Tell(
        IReadOnlyList<FoodMoment> moments, IReadOnlyList<RecipeReview> reviews, IReadOnlyList<Recipe> recipes, HistoryRequest request,
        TimeZoneInfo zone)
    {
        var library = recipes.ToDictionary(recipe => recipe.Id);
        var ordered = moments.OrderBy(moment => moment.OccurredOn).ThenBy(moment => moment.Id).ToList();
        var cooks = ordered.Where(moment => moment.Kind == FoodMomentKind.Cooked).ToList();

        var told = new List<HistoryMoment>();
        var totalDinners = TellCooking(cooks, library, zone, told);
        TellArrivals(ordered.Where(moment => moment.Kind == FoodMomentKind.RecipeAdded), library, zone, told);
        TellReviews(reviews, library, zone, told);
        foreach (var moment in ordered)
        {
            if (Plain(moment, zone) is { } plain)
            {
                told.Add(plain);
            }
        }

        var chapters = told
            .GroupBy(moment => MonthOf(DayOf(moment)))
            .OrderByDescending(month => month.Key)
            .Select(month => Chapter(month.Key, month.ToList(), cooks, library))
            .ToList();

        if (request.Before is { } before)
        {
            chapters = chapters.Where(chapter => chapter.Month < MonthOf(before)).ToList();
        }

        var size = request.Months <= 0 ? DefaultMonths : Math.Min(request.Months, MaxMonths);
        var page = chapters.Take(size).ToList();
        var olderBefore = chapters.Count > page.Count ? page[^1].Month : (DateOnly?)null;
        return new HistoryResponse(page, olderBefore, ordered.FirstOrDefault()?.OccurredOn, totalDinners);
    }

    /// <summary>
    /// Adds the cooking to <paramref name="told"/>: special cooks on their own, the rest one moment per week.
    /// Returns how many dinners were cooked in all.
    /// </summary>
    private static int TellCooking(
        List<FoodMoment> cooks, Dictionary<Guid, Recipe> library, TimeZoneInfo zone, List<HistoryMoment> told)
    {
        var cooked = new Dictionary<string, (int Times, DateOnly Last)>();
        var everyday = new List<FoodMoment>();
        var dinners = 0;
        foreach (var cook in cooks)
        {
            var key = DishKey(cook);
            var seen = cooked.TryGetValue(key, out var before);
            var times = seen ? before.Times + 1 : 1;
            cooked[key] = (times, cook.OccurredOn);
            var link = Linked(cook.RecipeId, library);

            HistoryMoment? special = null;
            if (!seen && cook.RecipeId is not null)
            {
                special = Cook(cook, HistoryMomentKind.Debut, link, null, zone);
            }
            else if (FanFavouriteCooks.Contains(times))
            {
                special = Cook(cook, HistoryMomentKind.FanFavourite, link, times, zone);
            }
            else if (seen && cook.OccurredOn.DayNumber - before.Last.DayNumber >= ComebackDays)
            {
                var days = cook.OccurredOn.DayNumber - before.Last.DayNumber;
                special = Cook(cook, HistoryMomentKind.Comeback, link, days, zone);
            }
            else if (cook.FavouredBy.Count > 0)
            {
                special = Cook(cook, HistoryMomentKind.FavouriteCooked, link, null, zone);
            }

            if (special is null)
            {
                everyday.Add(cook);
            }
            else
            {
                told.Add(special);
            }

            if (cook.Slot == MealSlot.Dinner && DinnerMilestones.Contains(++dinners))
            {
                told.Add(Cook(cook, HistoryMomentKind.DinnerMilestone, link, dinners, zone));
            }
        }

        // A week that runs over the end of a month is split, so each chapter keeps its own plates.
        foreach (var week in everyday.GroupBy(cook => (Month: MonthOf(cook.OccurredOn), Week: WeekStartOf(cook.OccurredOn))))
        {
            var dishes = week.Select(cook => new HistoryDish(cook.Title, Linked(cook.RecipeId, library), cook.OccurredOn)).ToList();
            told.Add(new HistoryMoment(
                At(week.Last(), zone), HistoryMomentKind.WeekOfPlates, null, null, dishes.Count, dishes, []));
        }

        return dinners;
    }

    /// <summary>A moment about one cook, naming whose favourite it was.</summary>
    private static HistoryMoment Cook(FoodMoment cook, HistoryMomentKind kind, Guid? link, int? count, TimeZoneInfo zone) =>
        new(At(cook, zone), kind, cook.Title, link, count, [], [.. cook.FavouredBy]);

    /// <summary>Adds the recipes that joined: one moment each, or a stampede for a day of many.</summary>
    private static void TellArrivals(
        IEnumerable<FoodMoment> added, Dictionary<Guid, Recipe> library, TimeZoneInfo zone, List<HistoryMoment> told)
    {
        foreach (var day in added.GroupBy(moment => moment.OccurredOn))
        {
            var arrivals = day.ToList();
            if (arrivals.Count >= StampedeSize)
            {
                var named = arrivals.Take(StampedeNames)
                    .Select(moment => new HistoryDish(moment.Title, Linked(moment.RecipeId, library), moment.OccurredOn))
                    .ToList();
                told.Add(new HistoryMoment(
                    At(arrivals[^1], zone), HistoryMomentKind.RecipeStampede, null, null, arrivals.Count, named, []));
                continue;
            }

            told.AddRange(arrivals.Select(moment => new HistoryMoment(
                At(moment, zone), HistoryMomentKind.RecipeAdded, moment.Title, Linked(moment.RecipeId, library), null, [], [])));
        }
    }

    /// <summary>Adds a moment for each review, under the recipe's name. A recipe gone from the database keeps no name, so its reviews are left out.</summary>
    private static void TellReviews(
        IReadOnlyList<RecipeReview> reviews, Dictionary<Guid, Recipe> library, TimeZoneInfo zone, List<HistoryMoment> told)
    {
        foreach (var review in reviews.OrderBy(review => review.CreatedAt).ThenBy(review => review.Id))
        {
            if (library.TryGetValue(review.RecipeId, out var recipe))
            {
                told.Add(new HistoryMoment(
                    At(review.ReviewedOn, review.CreatedAt, zone), HistoryMomentKind.Reviewed, recipe.Name, Linked(recipe.Id, library), null, [], [],
                    new HistoryReview(review.Stars, review.Notes, review.ReviewerName)));
            }
        }
    }

    /// <summary>The moments told as they were stored. Null for cooking and arrivals, which are told above.</summary>
    private static HistoryMoment? Plain(FoodMoment moment, TimeZoneInfo zone) => moment.Kind switch
    {
        FoodMomentKind.RecipeRetired => new(At(moment, zone), HistoryMomentKind.RecipeRetired, moment.Title, null, null, [], []),
        FoodMomentKind.Haul => new(At(moment, zone), HistoryMomentKind.Haul, null, null, moment.Count, [], []),
        FoodMomentKind.KitchenOpened => new(At(moment, zone), HistoryMomentKind.KitchenOpened, moment.Title, null, null, [], []),
        FoodMomentKind.PersonJoined => new(At(moment, zone), HistoryMomentKind.PersonJoined, moment.Title, null, null, [], []),
        _ => null,
    };

    private static HistoryChapter Chapter(
        DateOnly month, List<HistoryMoment> moments, List<FoodMoment> cooks, Dictionary<Guid, Recipe> library)
    {
        var cooked = cooks.Where(cook => MonthOf(cook.OccurredOn) == month).ToList();
        var dinners = cooked.Count(cook => cook.Slot == MealSlot.Dinner);

        var top = cooked.GroupBy(DishKey)
            .Select(dish => (Title: dish.Last().Title, Times: dish.Count()))
            .OrderByDescending(dish => dish.Times)
            .FirstOrDefault(dish => dish.Times > 1);

        var topTag = cooked
            .SelectMany(cook => cook.RecipeId is Guid id && library.TryGetValue(id, out var recipe) ? recipe.Tags : Enumerable.Empty<string>())
            .GroupBy(tag => tag.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(tag => tag.Key.Length > 0 && tag.Count() > 1)
            .OrderByDescending(tag => tag.Count())
            .Select(tag => tag.Key)
            .FirstOrDefault();

        // Newest first, so a moment with only a day, at its midnight, goes after the rest of that day. Ties keep
        // the order they were told in, reversed.
        var newestFirst = moments
            .Select((moment, index) => (moment, index))
            .OrderByDescending(pair => pair.moment.At)
            .ThenByDescending(pair => pair.index)
            .Select(pair => pair.moment)
            .ToList();
        return new HistoryChapter(month, newestFirst, dinners, top.Title, top.Times, topTag);
    }

    /// <summary>What makes two cooks the same dish: the recipe, or a free-form meal's name.</summary>
    private static string DishKey(FoodMoment cook) =>
        cook.RecipeId?.ToString() ?? "meal:" + cook.Title.Trim().ToLowerInvariant();

    /// <summary>The recipe to link to, when it is still in the library.</summary>
    private static Guid? Linked(Guid? id, Dictionary<Guid, Recipe> library) =>
        id is Guid recipeId && library.TryGetValue(recipeId, out var recipe) && !recipe.IsArchived ? recipeId : null;

    /// <summary>When a stored moment happened, in the household's time.</summary>
    private static DateTimeOffset At(FoodMoment moment, TimeZoneInfo zone) => At(moment.OccurredOn, moment.OccurredAt, zone);

    /// <summary>
    /// The exact time, in <paramref name="zone"/>, when it falls on <paramref name="day"/>; otherwise the day's
    /// midnight, as when something was recorded later than it happened.
    /// </summary>
    private static DateTimeOffset At(DateOnly day, DateTimeOffset? exact, TimeZoneInfo zone)
    {
        if (exact is { } time && TimeZoneInfo.ConvertTime(time, zone) is var local && DateOnly.FromDateTime(local.DateTime) == day)
        {
            return local;
        }

        var midnight = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(midnight, zone.GetUtcOffset(midnight));
    }

    /// <summary>The household's day a told moment is on.</summary>
    private static DateOnly DayOf(HistoryMoment moment) => DateOnly.FromDateTime(moment.At.DateTime);

    private static DateOnly MonthOf(DateOnly date) => new(date.Year, date.Month, 1);

    /// <summary>The Sunday a week starts on, as plans do.</summary>
    private static DateOnly WeekStartOf(DateOnly date) => date.AddDays(-(int)date.DayOfWeek);
}
