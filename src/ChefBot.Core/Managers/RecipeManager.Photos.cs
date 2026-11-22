using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>Reading recipes from photos with an AI model.</content>
internal sealed partial class RecipeManager
{
    public async Task<RecipePhotoResponse> ReadRecipePhotoAsync(RecipePhotoRequest request, CancellationToken cancellationToken)
    {
        var photos = ValidationEngine.RecipePhotos(request.Photos);
        var model = ValidationEngine.AiModelFor(AiTask.RecipeFromPhoto, await settings.GetAiAsync(cancellationToken));

        var answer = await ai.AskAsync(model, TransformationEngine.RecipePhotoQuestion(photos), cancellationToken);
        var text = TransformationEngine.ReadPhotoAnswer(answer, await WordsAsync(cancellationToken))
            ?? throw new RuleViolationException(
                "ChefBot couldn't find a recipe in that photo. Try a sharper photo with the whole recipe in view, or type it in.");
        return new RecipePhotoResponse(text.ToDraft(), model.Name);
    }
}
