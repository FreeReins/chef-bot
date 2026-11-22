using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies FoodMomentEntity to and from FoodMoment.</content>
internal static partial class EntityMapping
{
    public static FoodMoment ToContract(this FoodMomentEntity entity) => new()
    {
        Id = entity.Id,
        OccurredOn = entity.OccurredOn,
        OccurredAt = entity.OccurredAt,
        Kind = entity.Kind,
        RecipeId = entity.RecipeId,
        MealId = entity.MealId,
        Slot = entity.Slot,
        Title = entity.Title,
        Count = entity.Count,
        FavouredBy = [.. entity.FavouredBy],
    };

    public static FoodMomentEntity ToEntity(this FoodMoment moment) => new()
    {
        Id = moment.Id,
        OccurredOn = moment.OccurredOn,
        OccurredAt = moment.OccurredAt,
        Kind = moment.Kind,
        RecipeId = moment.RecipeId,
        MealId = moment.MealId,
        Slot = moment.Slot,
        Title = moment.Title,
        Count = moment.Count,
        FavouredBy = [.. moment.FavouredBy],
    };
}
