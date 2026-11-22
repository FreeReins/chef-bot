using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <content>The AI models and tasks to and from their requests and responses.</content>
internal static partial class Mapping
{
    /// <summary>The settings to save from a checked request. API keys left out keep the saved ones.</summary>
    public static AiSettings ToAiSettings(this AiSettingsRequest request, AiSettings current)
    {
        var defaultName = request.DefaultModel?.Trim() ?? string.Empty;
        var models = request.Models.Select((draft, index) =>
        {
            var saved = current.Models.FirstOrDefault(model => model.Id == draft.Id);
            return new AiModel
            {
                Id = saved?.Id ?? Guid.CreateVersion7(),
                Name = draft.Name.Trim(),
                Provider = draft.Provider,
                Model = draft.Model.Trim(),
                Endpoint = draft.Endpoint?.Trim() ?? string.Empty,
                ApiKey = draft.ApiKey?.Trim() ?? saved?.ApiKey ?? string.Empty,
                IsDefault = string.Equals(draft.Name.Trim(), defaultName, StringComparison.OrdinalIgnoreCase),
                Position = index,
            };
        }).ToList();

        var routes = (request.Tasks ?? [])
            .Where(choice => !string.IsNullOrWhiteSpace(choice.Model))
            .Select(choice => (choice.Task, Model: models.First(model => string.Equals(model.Name, choice.Model!.Trim(), StringComparison.OrdinalIgnoreCase))))
            .Where(choice => !choice.Model.IsDefault)
            .DistinctBy(choice => choice.Task)
            .Select(choice => new AiRoute { Task = choice.Task, ModelId = choice.Model.Id })
            .ToList();
        return new AiSettings { Models = models, Routes = routes };
    }

    public static AiSettingsResponse ToResponse(this AiSettings settings) => new(
        settings.Models.Select(model => model.ToResponse()).ToList(),
        settings.Models.FirstOrDefault(model => model.IsDefault)?.Name,
        Enum.GetValues<AiTask>().Select(task => new AiTaskResponse(
            task,
            AiTasks.Label(task),
            AiTasks.Description(task),
            settings.Routes.FirstOrDefault(route => route.Task == task) is { } route
                ? settings.Models.FirstOrDefault(model => model.Id == route.ModelId)?.Name
                : null)).ToList());

    public static AiModelResponse ToResponse(this AiModel model) =>
        new(model.Id, model.Name, model.Provider, model.Model, model.Endpoint, model.ApiKey.Length > 0);

    public static RecipeSuggestionResponse ToResponse(this RecipeSuggestion suggestion, bool stale) =>
        new(suggestion.RecipeId, suggestion.ModelName, suggestion.CreatedAt, suggestion.Status, stale, suggestion.Changes);
}
