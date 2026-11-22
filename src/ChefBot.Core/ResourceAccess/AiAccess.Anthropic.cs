using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess;

/// <content>Asking Claude, through Anthropic's SDK.</content>
internal sealed partial class AiAccess
{
    /// <summary>
    /// Claude models that accept server-side refusal fallbacks: when one declines a request, Anthropic answers it with
    /// another model in the same call instead of stopping.
    /// </summary>
    private static readonly HashSet<string> FallbackModels =
        ["claude-opus-5-5", "claude-opus-5", "claude-sonnet-5-5", "claude-fable-5-1", "claude-fable-5-2"];

    private async Task<string> AskAnthropicAsync(AiModel model, AiQuestion question, CancellationToken cancellationToken)
    {
        IAnthropicClient client = new AnthropicClient
        {
            ApiKey = model.ApiKey,
            HttpClient = http,
        };
        if (model.Endpoint.Length > 0)
        {
            client = client.WithOptions(options => options with { BaseUrl = model.Endpoint });
        }

        List<BetaContentBlockParam> content =
        [
            .. question.Images.Select(image => (BetaContentBlockParam)new BetaImageBlockParam
            {
                Source = new BetaBase64ImageSource { Data = Convert.ToBase64String(image.Data), MediaType = image.MediaType },
            }),
            new BetaTextBlockParam { Text = question.Text },
        ];
        var parameters = new MessageCreateParams
        {
            Model = model.Model,
            MaxTokens = 16000,
            System = question.Instructions,
            Messages = [new BetaUserMessageParam { Content = content }],
            OutputConfig = new BetaOutputConfig
            {
                Format = new BetaJsonOutputFormat
                {
                    Schema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(question.JsonSchema)!,
                },
            },
        };
        if (FallbackModels.Contains(model.Model))
        {
            parameters = parameters with { Betas = [AnthropicBeta.ServerSideFallback2026_07_01], Fallbacks = new Default() };
        }

        BetaMessage message;
        try
        {
            message = await client.Beta.Messages.Create(parameters, cancellationToken);
        }
        catch (AnthropicUnauthorizedException exception)
        {
            throw new AiUnavailableException($"{model.Name} didn't accept the API key. Enter a new one in Settings, AI assistants.", false, exception);
        }
        catch (AnthropicNotFoundException exception)
        {
            throw new AiUnavailableException($"Anthropic doesn't know the model {model.Model}. Check its name in Settings, AI assistants.", false, exception);
        }
        catch (Exception exception) when (exception is AnthropicRateLimitException or Anthropic5xxException or AnthropicIOException)
        {
            LogUnreachable(logger, model.Name, exception);
            throw new AiUnavailableException($"{model.Name} is busy or couldn't be reached. Try again later.", true, exception);
        }
        catch (Anthropic4xxException exception)
        {
            LogUnreachable(logger, model.Name, exception);
            throw new AiUnavailableException($"{model.Name} refused the question: {exception.Message}", false, exception);
        }

        if (message.StopReason == BetaStopReason.Refusal)
        {
            throw new AiUnavailableException($"{model.Name} declined to answer. Try another photo or recipe, or another model.", false);
        }

        if (message.StopReason == BetaStopReason.MaxTokens)
        {
            throw new AiUnavailableException($"{model.Name}'s answer was cut off before it finished. Try again.", false);
        }

        var text = string.Concat(message.Content.Select(block => block.Value).OfType<BetaTextBlock>().Select(block => block.Text));
        return JsonObjectIn(text, model.Name);
    }
}
