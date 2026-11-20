using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ChefBot.Core.Contracts;
using ChefBot.Core.ResourceAccess.Contracts;
using Microsoft.Extensions.Logging;

namespace ChefBot.Core.ResourceAccess;

/// <summary>Reads recipes from Mealie's REST API. ChefBot only ever reads from Mealie.</summary>
internal sealed partial class RecipeSourceAccess(HttpClient http, ILogger<RecipeSourceAccess> logger) : IRecipeSourceAccess
{
    private const int PageSize = 100;
    private const int MaxParallelRequests = 6;

    /// <summary>Above this share of unreadable recipes the import is abandoned instead of saved half-done.</summary>
    private const double MaxFailureShare = 0.2;

    public async Task<RecipeSourceResult> FetchAllAsync(MealieSettings connection, CancellationToken cancellationToken)
    {
        if (!connection.IsConfigured)
        {
            throw new RecipeSourceException("Mealie isn't set up. Enter its address and an API token on the Settings tab.");
        }

        // The trailing slash keeps a path prefix such as /mealie/ when relative paths are added.
        var mealie = new Server(new Uri(connection.Url.TrimEnd('/') + "/"), new AuthenticationHeaderValue("Bearer", connection.ApiToken));
        var slugs = await ListSlugsAsync(mealie, cancellationToken);
        var recipes = new ConcurrentBag<SourceRecipe>();
        var failures = 0;

        var parallel = new ParallelOptions { MaxDegreeOfParallelism = MaxParallelRequests, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(slugs, parallel, async (slug, token) =>
        {
            try
            {
                var detail = await GetAsync(mealie, $"api/recipes/{Uri.EscapeDataString(slug)}", MealieJson.Default.MealieRecipe, token);
                if (Map(detail) is { } recipe)
                {
                    recipes.Add(recipe);
                }
            }
            catch (RecipeSourceException exception)
            {
                Interlocked.Increment(ref failures);
                logger.LogWarning("Skipped Mealie recipe {Slug}: {Reason}", slug, exception.Message);
            }
        });

        if (failures > slugs.Count * MaxFailureShare)
        {
            throw new RecipeSourceException($"Mealie couldn't provide {failures} of {slugs.Count} recipes, so nothing was imported.");
        }

        var ordered = recipes.OrderBy(recipe => recipe.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return new RecipeSourceResult(ordered, failures);
    }

    private async Task<List<string>> ListSlugsAsync(Server mealie, CancellationToken cancellationToken)
    {
        var slugs = new List<string>();
        for (var page = 1; ; page++)
        {
            var result = await GetAsync(mealie, $"api/recipes?page={page}&perPage={PageSize}", MealieJson.Default.MealiePage, cancellationToken);
            var items = result.Items ?? [];
            slugs.AddRange(items.Select(item => item.Slug).OfType<string>().Where(slug => slug.Length > 0));
            if (items.Count == 0 || page >= result.PageCount)
            {
                return slugs;
            }
        }
    }

    private async Task<T> GetAsync<T>(Server mealie, string path, JsonTypeInfo<T> shape, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(mealie.BaseAddress, path));
            request.Headers.Authorization = mealie.Authorization;
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new RecipeSourceException($"Mealie answered HTTP {(int)response.StatusCode} for {path}.");
            }

            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync(body, shape, cancellationToken)
                ?? throw new RecipeSourceException($"Mealie sent an empty answer for {path}.");
        }
        catch (HttpRequestException exception)
        {
            throw new RecipeSourceException($"Mealie couldn't be reached: {exception.Message}", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RecipeSourceException("Mealie took too long to answer.", exception);
        }
        catch (JsonException exception)
        {
            throw new RecipeSourceException($"Mealie sent something unreadable for {path}. Check Mealie's address on the Settings tab.", exception);
        }
    }

    private static SourceRecipe? Map(MealieRecipe recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.Id) || string.IsNullOrWhiteSpace(recipe.Name))
        {
            return null;
        }

        var labels = new[] { recipe.Tags, recipe.RecipeCategory, recipe.Tools }
            .SelectMany(group => group ?? [])
            .Select(label => label.Name)
            .OfType<string>()
            .ToList();

        var ingredients = (recipe.RecipeIngredient ?? [])
            .Select(row => new SourceIngredient(
                row.Food?.Name,
                row.Quantity,
                row.Unit?.Name,
                FirstText(row.OriginalText, row.Note, row.Display)))
            .ToList();

        var servings = recipe.RecipeServings > 0 ? recipe.RecipeServings : recipe.RecipeYieldQuantity;
        return new SourceRecipe(recipe.Id, recipe.Name, servings > 0 ? servings : null, labels, ingredients);
    }

    private static string? FirstText(params string?[] candidates) =>
        candidates.FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

    /// <summary>The Mealie instance one import talks to.</summary>
    private sealed record Server(Uri BaseAddress, AuthenticationHeaderValue Authorization);

    // The parts of Mealie's JSON that ChefBot reads. Everything is optional, so an
    // unexpected or missing field leaves a gap instead of failing the whole import.

    private sealed class MealiePage
    {
        public List<MealieSummary>? Items { get; set; }

        [JsonPropertyName("total_pages")]
        public int? TotalPagesSnakeCase { get; set; }

        public int? TotalPages { get; set; }

        [JsonIgnore]
        public int PageCount => TotalPagesSnakeCase ?? TotalPages ?? 1;
    }

    private sealed class MealieSummary
    {
        public string? Slug { get; set; }
    }

    private sealed class MealieRecipe
    {
        public string? Id { get; set; }

        public string? Slug { get; set; }

        public string? Name { get; set; }

        public decimal? RecipeServings { get; set; }

        public decimal? RecipeYieldQuantity { get; set; }

        public List<MealieNamed>? Tags { get; set; }

        public List<MealieNamed>? RecipeCategory { get; set; }

        public List<MealieNamed>? Tools { get; set; }

        public List<MealieIngredient>? RecipeIngredient { get; set; }
    }

    private sealed class MealieIngredient
    {
        public decimal? Quantity { get; set; }

        public MealieNamed? Unit { get; set; }

        public MealieNamed? Food { get; set; }

        public string? Note { get; set; }

        public string? OriginalText { get; set; }

        public string? Display { get; set; }
    }

    private sealed class MealieNamed
    {
        public string? Name { get; set; }
    }

    /// <summary>The JSON shapes ChefBot reads from Mealie, made at build time. Mealie sometimes sends numbers as strings.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web, NumberHandling = JsonNumberHandling.AllowReadingFromString)]
    [JsonSerializable(typeof(MealiePage))]
    [JsonSerializable(typeof(MealieRecipe))]
    private sealed partial class MealieJson : JsonSerializerContext;
}
