using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>The AI models ChefBot may use, which does each task, and which AI features are ready.</summary>
public static class AiEndpoints
{
    /// <summary>The AI models, without their API keys, the default, and which model does each task.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverineGet("/api/ai/settings")]
    public static Task<AiSettingsResponse> GetSettings([FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.GetAiSettingsAsync(cancellationToken);

    /// <summary>
    /// Makes the AI models exactly these, and chooses the default and which model does each task. An API key left out
    /// keeps the saved one. Answers 400 when a model is missing something.
    /// </summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverinePut("/api/ai/settings")]
    public static Task<AiSettingsResponse> SaveSettings(
        AiSettingsRequest request, [FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.SaveAiSettingsAsync(request, cancellationToken);

    /// <summary>Asks a saved model a short question. A model that can't be reached is reported in the answer, not as an error.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverinePost("/api/ai/models/{id}/test")]
    public static Task<AiModelTestResult> TestModel(Guid id, [FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.TestAiModelAsync(id, cancellationToken);

    /// <summary>Which AI features are ready to use, so the page offers only those.</summary>
    [WolverineGet("/api/ai/features")]
    public static Task<AiFeaturesResponse> GetFeatures([FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.GetAiFeaturesAsync(cancellationToken);
}
