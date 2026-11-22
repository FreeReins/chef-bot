using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace ChefBot.Core.ResourceAccess;

/// <summary>
/// Asks AI models questions, one way per <see cref="AiProvider"/>:
/// <list type="bullet">
/// <item>Anthropic: the Messages API through Anthropic's own SDK, with the answer held to the question's JSON schema.</item>
/// <item>OpenAI-compatible: <c>POST {endpoint}/chat/completions</c> with images as data addresses and
/// <c>response_format</c> set to the question's JSON schema. Services that ignore the schema still usually answer with
/// JSON, so the answer is read leniently.</item>
/// </list>
/// </summary>
internal sealed partial class AiAccess(HttpClient http, ILogger<AiAccess> logger) : IAiAccess
{
    public Task<string> AskAsync(AiModel model, AiQuestion question, CancellationToken cancellationToken) =>
        model.Provider == AiProvider.Anthropic
            ? AskAnthropicAsync(model, question, cancellationToken)
            : AskOpenAiAsync(model, question, cancellationToken);

    /// <summary>The first JSON object in a model's answer, without any ```json fence or words around it.</summary>
    /// <exception cref="AiUnavailableException">There is no JSON object in the answer.</exception>
    internal static string JsonObjectIn(string? answer, string modelName)
    {
        var text = answer ?? string.Empty;
        var start = text.IndexOf('{', StringComparison.Ordinal);
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            var candidate = text[start..(end + 1)];
            try
            {
                using var parsed = JsonDocument.Parse(candidate);
                if (parsed.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return candidate;
                }
            }
            catch (JsonException)
            {
                // Falls through to the error below.
            }
        }

        throw new AiUnavailableException($"{modelName} didn't answer in a form ChefBot can read. Try again, or choose a more capable model.", false);
    }

    private async Task<string> AskOpenAiAsync(AiModel model, AiQuestion question, CancellationToken cancellationToken)
    {
        var content = new JsonArray();
        foreach (var image in question.Images)
        {
            content.Add(new JsonObject
            {
                ["type"] = "image_url",
                ["image_url"] = new JsonObject { ["url"] = $"data:{image.MediaType};base64,{Convert.ToBase64String(image.Data)}" },
            });
        }

        content.Add(new JsonObject { ["type"] = "text", ["text"] = question.Text });
        var body = new JsonObject
        {
            ["model"] = model.Model,
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = question.Instructions },
                new JsonObject { ["role"] = "user", ["content"] = content },
            },
            ["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject
                {
                    ["name"] = question.SchemaName,
                    ["strict"] = true,
                    ["schema"] = JsonNode.Parse(question.JsonSchema),
                },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ChatAddress(model.Endpoint))
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        if (model.ApiKey.Length > 0)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", model.ApiKey);
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            LogUnreachable(logger, model.Name, exception);
            throw new AiUnavailableException($"ChefBot couldn't reach {model.Name}. Check its address in Settings, AI assistants, and that it is running.", true, exception);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogRefused(logger, model.Name, (int)response.StatusCode, text.Length > 500 ? text[..500] : text);
                throw Refused(model, response.StatusCode);
            }

            string? answer;
            try
            {
                var choice = JsonNode.Parse(text)?["choices"]?[0];
                if (choice?["finish_reason"]?.GetValue<string>() == "length")
                {
                    throw new AiUnavailableException($"{model.Name}'s answer was cut off before it finished. Try again, or choose a model that can write longer answers.", false);
                }

                answer = choice?["message"]?["content"]?.GetValue<string>();
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException)
            {
                throw new AiUnavailableException($"{model.Name} answered with something that isn't a chat completion. Check that its address ends in /v1.", false, exception);
            }

            return JsonObjectIn(answer, model.Name);
        }
    }

    /// <summary>The chat completions address under an OpenAI-compatible service's address, such as http://ollama:11434/v1.</summary>
    private static Uri ChatAddress(string endpoint) => new(endpoint.TrimEnd('/') + "/chat/completions");

    private static AiUnavailableException Refused(AiModel model, HttpStatusCode status) => (int)status switch
    {
        401 or 403 => new($"{model.Name} didn't accept the API key. Enter a new one in Settings, AI assistants.", false),
        404 => new($"{model.Name} doesn't know the model {model.Model}, or its address is wrong. Check both in Settings, AI assistants.", false),
        429 => new($"{model.Name} is busy or out of credit. Try again later.", true),
        >= 500 => new($"{model.Name} had a problem answering (HTTP {(int)status}). Try again later.", true),
        _ => new($"{model.Name} refused the question (HTTP {(int)status}). It may not accept images or JSON answers; try another model.", false),
    };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Couldn't reach the AI model {Model}")]
    private static partial void LogUnreachable(ILogger logger, string model, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The AI model {Model} answered HTTP {Status}: {Body}")]
    private static partial void LogRefused(ILogger logger, string model, int status, string body);
}
