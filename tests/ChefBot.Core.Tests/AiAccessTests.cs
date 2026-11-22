using System.Net;
using System.Text.Json;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChefBot.Core.Tests;

/// <summary>What an OpenAI-compatible service is sent, and how its answers are read, against a recording stand-in.</summary>
public class AiAccessTests
{
    private static readonly AiModel Ollama = new() { Name = "Ollama", Provider = AiProvider.OpenAiCompatible, Model = "qwen2.5vl", Endpoint = "http://ollama.local:11434/v1/" };

    private static readonly AiQuestion Question = new(
        "Read the recipe.", "Here it is.", [new AiImage([0xFF, 0xD8, 0xFF, 0x00], "image/jpeg")],
        """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"],"additionalProperties":false}""", "recipe_photo");

    private static (AiAccess Access, RecordingHandler Server) Connect(string reply, HttpStatusCode status = HttpStatusCode.OK)
    {
        var server = new RecordingHandler { Reply = reply, Status = status };
        return (new AiAccess(new HttpClient(server), NullLogger<AiAccess>.Instance), server);
    }

    private static string Completion(string content) =>
        JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = "stop", message = new { content } } } });

    [Test]
    public async Task Images_and_the_schema_go_to_chat_completions()
    {
        var (access, server) = Connect(Completion("""{"name":"Soup"}"""));

        var answer = await access.AskAsync(Ollama, Question, default);

        await Assert.That(answer).IsEqualTo("""{"name":"Soup"}""");
        var (request, body) = server.Requests.Single();
        await Assert.That(request.RequestUri!.AbsoluteUri).IsEqualTo("http://ollama.local:11434/v1/chat/completions");
        await Assert.That(request.Headers.Authorization).IsNull();
        using var sent = JsonDocument.Parse(body);
        var content = sent.RootElement.GetProperty("messages")[1].GetProperty("content");
        await Assert.That(content[0].GetProperty("image_url").GetProperty("url").GetString()).IsEqualTo("data:image/jpeg;base64,/9j/AA==");
        await Assert.That(sent.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("name").GetString()).IsEqualTo("recipe_photo");
    }

    [Test]
    public async Task A_fenced_answer_is_still_read_and_a_key_is_sent_when_saved()
    {
        var (access, server) = Connect(Completion("Here you go:\n```json\n{\"name\":\"Soup\"}\n```"));

        var answer = await access.AskAsync(new AiModel { Name = "OpenAI", Provider = AiProvider.OpenAiCompatible, Model = "gpt", Endpoint = "https://api.openai.com/v1", ApiKey = "sk" }, Question, default);

        await Assert.That(answer).IsEqualTo("""{"name":"Soup"}""");
        await Assert.That(server.Requests.Single().Request.Headers.Authorization!.ToString()).IsEqualTo("Bearer sk");
    }

    [Test]
    public async Task Refusals_say_what_to_do_and_whether_to_try_again()
    {
        var wrongKey = (await TestData.Caught<AiUnavailableException>(() => Connect("{}", HttpStatusCode.Unauthorized).Access.AskAsync(Ollama, Question, default)))!;
        var busy = (await TestData.Caught<AiUnavailableException>(() => Connect("{}", HttpStatusCode.TooManyRequests).Access.AskAsync(Ollama, Question, default)))!;
        var prose = (await TestData.Caught<AiUnavailableException>(() => Connect(Completion("I can't read that.")).Access.AskAsync(Ollama, Question, default)))!;

        await Assert.That(wrongKey.Message).Contains("API key");
        await Assert.That(wrongKey.IsTransient).IsFalse();
        await Assert.That(busy.IsTransient).IsTrue();
        await Assert.That(prose.Message).Contains("didn't answer in a form ChefBot can read");
    }
}
