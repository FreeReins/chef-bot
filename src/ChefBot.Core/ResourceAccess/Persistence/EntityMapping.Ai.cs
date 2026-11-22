using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies the AI model and route entities to and from their contracts.</content>
internal static partial class EntityMapping
{
    public static AiModel ToContract(this AiModelEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Provider = entity.Provider,
        Model = entity.Model,
        Endpoint = entity.Endpoint,
        ApiKey = entity.ApiKey,
        IsDefault = entity.IsDefault,
        Position = entity.Position,
    };

    public static AiModelEntity ToEntity(this AiModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Provider = model.Provider,
        Model = model.Model,
        Endpoint = model.Endpoint,
        ApiKey = model.ApiKey,
        IsDefault = model.IsDefault,
        Position = model.Position,
    };

    public static AiRoute ToContract(this AiRouteEntity entity) => new() { Task = entity.Task, ModelId = entity.ModelId };

    public static AiRouteEntity ToEntity(this AiRoute route) => new() { Task = route.Task, ModelId = route.ModelId };
}
