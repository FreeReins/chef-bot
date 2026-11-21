using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IHistoryAccess"/>
internal sealed class HistoryAccess(ChefBotDbContext db) : IHistoryAccess
{
    public async Task<List<FoodMoment>> ListAsync(CancellationToken cancellationToken) =>
        (await db.FoodMoments.OrderBy(moment => moment.OccurredOn).ThenBy(moment => moment.Id).ToListAsync(cancellationToken))
        .Select(moment => moment.ToContract())
        .ToList();

    public Task AddAsync(FoodMoment moment, CancellationToken cancellationToken)
    {
        db.FoodMoments.Add(moment.ToEntity());
        return Task.CompletedTask;
    }

    public async Task RemoveForMealAsync(Guid mealId, CancellationToken cancellationToken)
    {
        // Loading them tracks them, so one added earlier in this request is found too.
        await db.FoodMoments.Where(moment => moment.MealId == mealId).LoadAsync(cancellationToken);
        db.FoodMoments.RemoveRange(db.FoodMoments.Local.Where(moment => moment.MealId == mealId).ToList());
    }
}
