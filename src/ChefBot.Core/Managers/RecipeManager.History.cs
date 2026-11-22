using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>
/// Writing the food history from the events that make it. Each event carries what its moment remembers, so a moment
/// written late is the same as one written at once. Recording the same event again adds nothing: other moments
/// remember the message they came from, and a cook follows the meal's status as it is now, so a cook and its undo
/// give the right history whichever arrives first.
/// </content>
internal sealed partial class RecipeManager
{
    public async Task RecordHistoryAsync(IKitchenMoment moment, Guid sourceId, CancellationToken cancellationToken)
    {
        var changed = moment switch
        {
            MealCooked cooked => await RecordCookingAsync(cooked.PlanId, cooked.MealId, cooked, sourceId, cancellationToken),
            MealUncooked uncooked => await RecordCookingAsync(uncooked.PlanId, uncooked.MealId, null, sourceId, cancellationToken),
            _ when await history.HasSourceAsync(sourceId, cancellationToken) => false,
            ShoppingDone haul => await AddMomentAsync(
                new FoodMoment { Kind = FoodMomentKind.Haul, OccurredOn = haul.OccurredOn, OccurredAt = haul.OccurredAt, Count = haul.Count },
                sourceId,
                cancellationToken),
            RecipesAdded added => await RecordRecipesAsync(FoodMomentKind.RecipeAdded, added.RecipeIds, added.OccurredOn, added.OccurredAt, sourceId, cancellationToken),
            RecipeArchived archived => await RecordRecipesAsync(FoodMomentKind.RecipeRetired, [archived.RecipeId], archived.OccurredOn, archived.OccurredAt, sourceId, cancellationToken),
            MemberJoined joined => await AddMomentAsync(
                new FoodMoment { Kind = FoodMomentKind.PersonJoined, OccurredOn = joined.OccurredOn, OccurredAt = joined.OccurredAt, Title = joined.DisplayName },
                sourceId,
                cancellationToken),
            KitchenOpened opened => await AddMomentAsync(
                new FoodMoment { Kind = FoodMomentKind.KitchenOpened, OccurredOn = opened.OccurredOn, OccurredAt = opened.OccurredAt, Title = opened.DisplayName },
                sourceId,
                cancellationToken),
            _ => false,
        };

        if (changed)
        {
            await messages.PublishAsync(new HistoryRecorded());
            await messages.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Makes the history match whether the meal is cooked now: one moment while it is (or while its plan is gone, since
    /// clearing a plan keeps its cooking), none once it was taken back. Leftovers never get one. <c>cooked</c> is the
    /// cook to record when the meal is cooked, or null when "cooked" was taken back.
    /// </summary>
    private async Task<bool> RecordCookingAsync(Guid planId, Guid mealId, MealCooked? cooked, Guid sourceId, CancellationToken cancellationToken)
    {
        var meal = (await plans.FindAsync(planId, cancellationToken))?.Meals.FirstOrDefault(meal => meal.Id == mealId);
        var isCooked = meal is null ? cooked is not null : meal.Status == MealStatus.Cooked;
        if (cooked is null && isCooked)
        {
            return false; // cooked again since: the newer cook's own event records it
        }

        await history.RemoveForMealAsync(mealId, cancellationToken);
        if (cooked is null || !isCooked || cooked.IsLeftover)
        {
            return true;
        }

        return await AddMomentAsync(
            new FoodMoment
            {
                Kind = FoodMomentKind.Cooked,
                OccurredOn = cooked.OccurredOn,
                OccurredAt = cooked.OccurredAt,
                RecipeId = cooked.RecipeId,
                MealId = cooked.MealId,
                Slot = cooked.Slot,
                Title = cooked.Title,
                FavouredBy = [.. cooked.FavouredBy],
            },
            sourceId,
            cancellationToken);
    }

    /// <summary>A moment for each recipe still in the library, named as it is now.</summary>
    private async Task<bool> RecordRecipesAsync(
        FoodMomentKind kind, IReadOnlyList<Guid> recipeIds, DateOnly on, DateTimeOffset at, Guid sourceId, CancellationToken cancellationToken)
    {
        var any = false;
        foreach (var id in recipeIds)
        {
            if (await recipes.FindAsync(id, cancellationToken) is { } recipe)
            {
                any |= await AddMomentAsync(
                    new FoodMoment { Kind = kind, OccurredOn = on, OccurredAt = at, RecipeId = recipe.Id, Title = recipe.Name }, sourceId, cancellationToken);
            }
        }

        return any;
    }

    private async Task<bool> AddMomentAsync(FoodMoment moment, Guid sourceId, CancellationToken cancellationToken)
    {
        moment.SourceId = sourceId;
        await history.AddAsync(moment, cancellationToken);
        return true;
    }
}
