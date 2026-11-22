using System.Text.Json;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies PlanStyleEntity to and from PlanStyle.</content>
internal static partial class EntityMapping
{
    public static PlanStyle ToContract(this PlanStyleEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        IsDefault = entity.IsDefault,
        Position = entity.Position,
        CookingDays = [.. entity.CookingDays],
        LeftoverLunches = entity.LeftoverLunches,
        Breakfasts = entity.Breakfasts,
        Snacks = entity.Snacks,
        Sweets = entity.Sweets,
        Drinks = entity.Drinks,
        Favourites = entity.Favourites,
        Rules = JsonSerializer.Deserialize(entity.Rules, RuleJson.Default.ListPlanRule) ?? [],
    };

    public static PlanStyleEntity ToEntity(this PlanStyle style) => new()
    {
        Id = style.Id,
        Name = style.Name,
        Description = style.Description,
        IsDefault = style.IsDefault,
        Position = style.Position,
        CookingDays = [.. style.CookingDays],
        LeftoverLunches = style.LeftoverLunches,
        Breakfasts = style.Breakfasts,
        Snacks = style.Snacks,
        Sweets = style.Sweets,
        Drinks = style.Drinks,
        Favourites = style.Favourites,
        Rules = JsonSerializer.Serialize(style.Rules, RuleJson.Default.ListPlanRule),
    };
}
