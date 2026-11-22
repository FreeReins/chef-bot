using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Setting up AI models, choosing which does each task, and testing one.</summary>
public class AiSettingsTests
{
    private static readonly AiModelDraft Claude = new(null, "Claude", AiProvider.Anthropic, "claude-opus-5-5", ApiKey: "sk-ant-secret");

    private static readonly AiModelDraft Ollama = new(null, "Ollama", AiProvider.OpenAiCompatible, "qwen2.5vl", "http://ollama.local:11434/v1");

    [Test]
    public async Task Saved_models_never_give_their_key_back()
    {
        var core = new TestCore();

        var saved = await core.MaintenanceManager().SaveAiSettingsAsync(new([Claude], "Claude"), default);

        var model = saved.Models.Single();
        await Assert.That(model.HasApiKey).IsTrue();
        await Assert.That(saved.DefaultModel).IsEqualTo("Claude");
        await Assert.That(core.Settings.Ai.Models.Single().ApiKey).IsEqualTo("sk-ant-secret");
        await Assert.That(core.Messages.Commits).IsEqualTo(1);
        var again = await core.MaintenanceManager().GetAiSettingsAsync(default);
        await Assert.That(again.Models.Single()).IsEqualTo(model);
    }

    [Test]
    public async Task A_key_left_out_is_kept_and_an_empty_one_removes_it()
    {
        var core = new TestCore();
        var id = (await core.MaintenanceManager().SaveAiSettingsAsync(new([Ollama with { ApiKey = "local" }], "Ollama"), default)).Models.Single().Id;

        await core.MaintenanceManager().SaveAiSettingsAsync(new([Ollama with { Id = id }], "Ollama"), default);
        await Assert.That(core.Settings.Ai.Models.Single().ApiKey).IsEqualTo("local");

        await core.MaintenanceManager().SaveAiSettingsAsync(new([Ollama with { Id = id, ApiKey = "" }], "Ollama"), default);
        await Assert.That(core.Settings.Ai.Models.Single().ApiKey).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Every_task_is_listed_and_one_can_use_another_model()
    {
        var core = new TestCore();

        var saved = await core.MaintenanceManager().SaveAiSettingsAsync(
            new([Claude, Ollama], "Claude", [new(AiTask.RecipeFromPhoto, "Ollama"), new(AiTask.RecipeDetails, "Claude")]), default);

        await Assert.That(string.Join(", ", saved.Tasks.Select(task => $"{task.Task}={task.Model ?? "default"}")))
            .IsEqualTo("RecipeFromPhoto=Ollama, RecipeDetails=default");
        await Assert.That(core.Settings.Ai.Routes.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Removing_a_model_sends_its_tasks_back_to_the_default()
    {
        var core = new TestCore();
        await core.MaintenanceManager().SaveAiSettingsAsync(new([Claude, Ollama], "Claude", [new(AiTask.RecipeFromPhoto, "Ollama")]), default);
        var claude = core.Settings.Ai.Models.Single(model => model.Name == "Claude");

        var saved = await core.MaintenanceManager().SaveAiSettingsAsync(new([Claude with { Id = claude.Id }], "Claude"), default);

        await Assert.That(saved.Tasks.All(task => task.Model is null)).IsTrue();
        await Assert.That(core.Settings.Ai.Routes).IsEmpty();
    }

    [Test]
    public async Task Models_need_what_their_provider_needs_and_a_default()
    {
        var core = new TestCore();
        var manager = core.MaintenanceManager();

        var noEndpoint = (await TestData.Caught<RuleViolationException>(() => manager.SaveAiSettingsAsync(new([Ollama with { Endpoint = null }], "Ollama"), default)))!;
        var noKey = (await TestData.Caught<RuleViolationException>(() => manager.SaveAiSettingsAsync(new([Claude with { ApiKey = null }], "Claude"), default)))!;
        var noDefault = (await TestData.Caught<RuleViolationException>(() => manager.SaveAiSettingsAsync(new([Claude]), default)))!;
        var twins = (await TestData.Caught<RuleViolationException>(() => manager.SaveAiSettingsAsync(new([Claude, Claude], "Claude"), default)))!;
        var unknown = (await TestData.Caught<RuleViolationException>(() =>
            manager.SaveAiSettingsAsync(new([Claude], "Claude", [new(AiTask.RecipeFromPhoto, "Gemini")]), default)))!;

        await Assert.That(noEndpoint.Message).Contains("/v1");
        await Assert.That(noKey.Message).Contains("API key");
        await Assert.That(noDefault.Message).Contains("default");
        await Assert.That(twins.Message).Contains("two AI models called Claude");
        await Assert.That(unknown.Message).Contains("no AI model called Gemini");
        await Assert.That(core.Settings.Ai.Models).IsEmpty();
    }

    [Test]
    public async Task Features_are_ready_once_any_model_is_set_up()
    {
        var core = new TestCore();
        await Assert.That(await core.MaintenanceManager().GetAiFeaturesAsync(default)).IsEqualTo(new AiFeaturesResponse(false, false));

        await core.MaintenanceManager().SaveAiSettingsAsync(new([Ollama], "Ollama"), default);

        await Assert.That(await core.MaintenanceManager().GetAiFeaturesAsync(default)).IsEqualTo(new AiFeaturesResponse(true, true));
    }

    [Test]
    public async Task A_test_reports_a_problem_instead_of_failing()
    {
        var core = new TestCore();
        var id = (await core.MaintenanceManager().SaveAiSettingsAsync(new([Claude], "Claude"), default)).Models.Single().Id;
        core.Ai.Answers["connection_check"] = """{"ok":true}""";

        await Assert.That(await core.MaintenanceManager().TestAiModelAsync(id, default)).IsEqualTo(new AiModelTestResult(true));

        core.Ai.Failure = new AiUnavailableException("Claude didn't accept the API key.", false);
        var failed = await core.MaintenanceManager().TestAiModelAsync(id, default);

        await Assert.That(failed).IsEqualTo(new AiModelTestResult(false, "Claude didn't accept the API key."));
        await Assert.That(core.Ai.Asked[0].Model.ApiKey).IsEqualTo("sk-ant-secret");
    }
}
