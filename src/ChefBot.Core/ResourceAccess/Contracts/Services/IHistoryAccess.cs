using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>The household's stored food history. Moments outlive the plans and recipes they mention.</summary>
internal interface IHistoryAccess
{
    /// <summary>Every moment, oldest first.</summary>
    Task<List<FoodMoment>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(FoodMoment moment, CancellationToken cancellationToken);

    /// <summary>Takes back the moments recorded for cooking a planned meal.</summary>
    Task RemoveForMealAsync(Guid mealId, CancellationToken cancellationToken);
}
