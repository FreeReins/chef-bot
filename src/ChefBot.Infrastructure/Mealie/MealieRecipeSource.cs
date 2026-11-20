using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.Abstractions;
using ChefBot.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChefBot.Infrastructure.Mealie;

/// <summary>Reads recipes from Mealie's REST API. ChefBot only ever reads from Mealie.</summary>
public sealed class MealieRecipeSource(HttpClient http, IOptions<MealieOptions> options, ILogger<MealieRecipeSource> logger)
    : IRecipeSource
{
    private const int PageSize = 100;
    private const int MaxParallelRequests = 6;

    /// <summary>Above this share of unreadable recipes the import is abandoned instead of saved half-done.</summary>
    private const double MaxFailureShare = 0.2;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public bool IsConfigured => options.Value.IsConfigured;

    public async Task<RecipeSourceResult> FetchAllAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new RecipeSourceException("Mealie isn't set up. Set Mealie__Url and Mealie__ApiToken to import from it.");
        }

        var slugs = await ListSlugsAsync(cancellationToken);
        var recipes = new ConcurrentBag<SourceRecipe>();
        var failures = 0;

        var parallel = new ParallelOptions { MaxDegreeOfParallelism = MaxParallelRequests, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(slugs, parallel, async (slug, token) =>
        {
            try
            {
                var detail = await GetAsync<MealieRecipe>($"api/recipes/{Uri.EscapeDataString(slug)}", token);
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

    private async Task<List<string>> ListSlugsAsync(CancellationToken cancellationToken)
    {
        var slugs = new List<string>();
        for (var page = 1; ; page++)
        {
            var result = await GetAsync<MealiePage>($"api/recipes?page={page}&perPage={PageSize}", cancellationToken);
            var items = result.Items ?? [];
            slugs.AddRange(items.Select(item => item.Slug).OfType<string>().Where(slug => slug.Length > 0));
            if (items.Count == 0 || page >= result.PageCount)
            {
                return slugs;
            }
        }
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using var response = await http.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new RecipeSourceException($"Mealie answered HTTP {(int)response.StatusCode} for {path}.");
            }

            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(body, Json, cancellationToken)
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
            throw new RecipeSourceException($"Mealie sent something unreadable for {path}. Check Mealie__Url.", exception);
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
}
