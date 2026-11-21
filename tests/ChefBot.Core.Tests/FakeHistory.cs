using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeHistory : IHistoryAccess
{
    public List<FoodMoment> Rows { get; } = [];

    public Task<List<FoodMoment>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Rows.OrderBy(moment => moment.OccurredOn).ToList());

    public Task AddAsync(FoodMoment moment, CancellationToken cancellationToken)
    {
        Rows.Add(moment);
        return Task.CompletedTask;
    }

    public Task RemoveForMealAsync(Guid mealId, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(moment => moment.MealId == mealId);
        return Task.CompletedTask;
    }
}
