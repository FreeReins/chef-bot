using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <content>Checks on the AI models and which does what.</content>
internal sealed partial class ValidationEngine
{
    private const int MaxAiModels = 10;

    private const int MaxAiModelName = 60;

    /// <summary>
    /// Checks the AI models to save: each has its own name and a model id, an OpenAI-compatible one has an address, an
    /// Anthropic one has an API key (a new one, or one already saved for it in <paramref name="current"/>), one of them
    /// is the default, and every task names one of them.
    /// </summary>
    /// <exception cref="RuleViolationException">Something is missing or doesn't match.</exception>
    public static AiSettingsRequest Ai(AiSettingsRequest request, AiSettings current)
    {
        var models = request.Models ?? [];
        if (models.Count > MaxAiModels)
        {
            throw new RuleViolationException($"Keep to {MaxAiModels} AI models or fewer.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var model in models)
        {
            var name = model.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                throw new RuleViolationException("Every AI model needs a name, such as Claude.");
            }

            if (name.Length > MaxAiModelName)
            {
                throw new RuleViolationException($"Keep AI model names to {MaxAiModelName} characters or fewer.");
            }

            if (!names.Add(name))
            {
                throw new RuleViolationException($"There are two AI models called {name}. Give each its own name.");
            }

            if (!Enum.IsDefined(model.Provider))
            {
                throw new RuleViolationException($"Choose how ChefBot talks to {name}: Anthropic, or an OpenAI-compatible service.");
            }

            if (string.IsNullOrWhiteSpace(model.Model))
            {
                throw new RuleViolationException($"Enter which model {name} is, such as claude-opus-5-5 or qwen2.5vl.");
            }

            var endpoint = model.Endpoint?.Trim() ?? string.Empty;
            if (model.Provider == AiProvider.OpenAiCompatible && !WebAddress.IsHttp(endpoint))
            {
                throw new RuleViolationException($"Enter the address {name} is reached at, ending in /v1, such as http://ollama.local:11434/v1.");
            }

            if (model.Provider == AiProvider.Anthropic && endpoint.Length > 0 && !WebAddress.IsHttp(endpoint))
            {
                throw new RuleViolationException($"The address for {name} must be a full http or https address, or empty to use Anthropic's own.");
            }

            var hasKey = model.ApiKey is { } key
                ? key.Trim().Length > 0
                : current.Models.Any(saved => saved.Id == model.Id && saved.ApiKey.Length > 0);
            if (model.Provider == AiProvider.Anthropic && !hasKey)
            {
                throw new RuleViolationException($"Enter an API key for {name}. Make one in the Claude Console, under API keys.");
            }
        }

        if (models.Count > 0 && !names.Contains(request.DefaultModel?.Trim() ?? string.Empty))
        {
            throw new RuleViolationException("Choose which AI model is the default.");
        }

        foreach (var choice in request.Tasks ?? [])
        {
            if (!Enum.IsDefined(choice.Task))
            {
                throw new RuleViolationException("One of the tasks isn't one ChefBot knows. Reload the page and try again.");
            }

            if (choice.Model is { } chosen && chosen.Trim().Length > 0 && !names.Contains(chosen.Trim()))
            {
                throw new RuleViolationException($"There is no AI model called {chosen.Trim()} for {AiTasks.Label(choice.Task)}. Choose one of the models above.");
            }
        }

        return request;
    }

    /// <summary>The model that does a task: the one chosen for it, or the default.</summary>
    /// <exception cref="RuleViolationException">No AI model is set up.</exception>
    public static AiModel AiModelFor(AiTask task, AiSettings settings) =>
        AiModelOrNull(task, settings)
        ?? throw new RuleViolationException("This needs an AI model. An admin can set one up under Settings, AI assistants.");

    /// <summary>The model that does a task, or null when none is set up.</summary>
    public static AiModel? AiModelOrNull(AiTask task, AiSettings settings) =>
        settings.Routes.FirstOrDefault(route => route.Task == task) is { } route
        && settings.Models.FirstOrDefault(model => model.Id == route.ModelId) is { } routed
            ? routed
            : settings.Models.FirstOrDefault(model => model.IsDefault) ?? settings.Models.FirstOrDefault();

    /// <summary>One of the saved AI models.</summary>
    /// <exception cref="NotFoundException">There is no saved model with that id.</exception>
    public static AiModel AiModel(AiSettings settings, Guid id) =>
        settings.Models.FirstOrDefault(model => model.Id == id)
        ?? throw new NotFoundException("That AI model isn't set up any more. Reload the page and choose another.");
}
