using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>The AI models ChefBot may use, and which does what.</content>
internal sealed partial class MaintenanceManager
{
    public async Task<AiSettingsResponse> GetAiSettingsAsync(CancellationToken cancellationToken) =>
        (await settings.GetAiAsync(cancellationToken)).ToResponse();

    public async Task<AiSettingsResponse> SaveAiSettingsAsync(AiSettingsRequest request, CancellationToken cancellationToken)
    {
        var current = await settings.GetAiAsync(cancellationToken);
        var fresh = ValidationEngine.Ai(request, current).ToAiSettings(current);

        await settings.SaveAiAsync(fresh, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return fresh.ToResponse();
    }

    public async Task<AiFeaturesResponse> GetAiFeaturesAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetAiAsync(cancellationToken);
        return new AiFeaturesResponse(
            ValidationEngine.AiModelOrNull(AiTask.RecipeFromPhoto, current) is not null,
            ValidationEngine.AiModelOrNull(AiTask.RecipeDetails, current) is not null);
    }

    public async Task<AiModelTestResult> TestAiModelAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = ValidationEngine.AiModel(await settings.GetAiAsync(cancellationToken), id);
        try
        {
            await ai.AskAsync(model, TransformationEngine.AiTestQuestion(), cancellationToken);
            return new AiModelTestResult(true);
        }
        catch (AiUnavailableException exception)
        {
            return new AiModelTestResult(false, exception.Message);
        }
    }
}
