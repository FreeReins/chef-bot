using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <content>Checks on recipes, their web pages and their photos.</content>
internal sealed partial class ValidationEngine
{
    public static void Recipe(RecipeText recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.Name))
        {
            throw new RuleViolationException("A recipe needs a name.");
        }

        if (recipe.Servings is <= 0m)
        {
            throw new RuleViolationException("Servings must be more than zero.");
        }

        if (!(recipe.Ingredients ?? []).Any(line => !string.IsNullOrWhiteSpace(line)))
        {
            throw new RuleViolationException("A recipe needs at least one ingredient.");
        }

        foreach (var (minutes, label) in new[] { (recipe.PrepMinutes, "Prep time"), (recipe.CookMinutes, "Cook time"), (recipe.TotalMinutes, "Total time") })
        {
            if (minutes is < 0 or > MaxMinutes)
            {
                throw new RuleViolationException($"{label} must be between 0 and {MaxMinutes} minutes (a week).");
            }
        }

        if (!string.IsNullOrWhiteSpace(recipe.SourceUrl) && !WebAddress.IsHttp(recipe.SourceUrl.Trim()))
        {
            throw new RuleViolationException("The source link must be a full web address starting with http:// or https://.");
        }
    }

    /// <summary>
    /// The address of a recipe page to import. An address typed without <c>https://</c>, such as
    /// <c>example.com/soup</c>, gets it added.
    /// </summary>
    public static Uri RecipePageAddress(string? url)
    {
        var trimmed = url?.Trim() ?? string.Empty;
        if (trimmed.Length > 0 && !trimmed.Contains("://", StringComparison.Ordinal))
        {
            trimmed = "https://" + trimmed;
        }

        return WebAddress.IsHttp(trimmed) && Uri.TryCreate(trimmed, UriKind.Absolute, out var address) && address.Host.Contains('.')
            ? address
            : throw new RuleViolationException("Enter the full address of a recipe page, such as https://example.com/recipes/soup.");
    }

    /// <summary>Refuses a page the library already has a recipe from, archived ones included, before it is downloaded.</summary>
    public static void PageNotInLibrary(string sourceUrl, IEnumerable<Recipe> library)
    {
        if (library.FirstOrDefault(existing => SamePage(existing.SourceUrl, sourceUrl)) is { } same)
        {
            throw new ConflictException($"That page is already in your recipes{Archived(same)} as {same.Name}, so nothing was added.");
        }
    }

    /// <summary>Refuses a recipe whose name the library already has, archived ones included, as file imports do.</summary>
    public static void NameNotInLibrary(RecipeText recipe, IEnumerable<Recipe> library)
    {
        if (library.FirstOrDefault(existing => existing.Name.Equals(recipe.Name.Trim(), StringComparison.OrdinalIgnoreCase)) is { } same)
        {
            throw new ConflictException($"{same.Name} is already in your recipes{Archived(same)}, so nothing was added. Rename that one to import this.");
        }
    }

    /// <summary>Checks a photo's bytes and says what kind of image it is, judged from the bytes, not the file name.</summary>
    /// <returns>The image's media type, such as image/jpeg.</returns>
    public static string Image(byte[]? data)
    {
        if (data is not { Length: > 0 })
        {
            throw new RuleViolationException("That image is empty. Choose a photo file and try again.");
        }

        if (data.Length > MaxImageBytes)
        {
            throw new RuleViolationException($"That image is larger than {MaxImageBytes / 1024 / 1024} MB. Choose a smaller one.");
        }

        return ImageFormat.Detect(data)
            ?? throw new RuleViolationException("That file isn't a photo ChefBot can show. Choose a JPEG, PNG, WebP or GIF image.");
    }

    /// <summary>Checks photos of a recipe to read: one to <see cref="MaxRecipePhotos"/> images ChefBot can show.</summary>
    /// <returns>The photos with their media types, in order.</returns>
    public static IReadOnlyList<AiImage> RecipePhotos(IReadOnlyList<byte[]>? photos)
    {
        if (photos is not { Count: > 0 })
        {
            throw new RuleViolationException("Choose a photo of the recipe first.");
        }

        if (photos.Count > MaxRecipePhotos)
        {
            throw new RuleViolationException($"Choose up to {MaxRecipePhotos} photos: the pages of one recipe.");
        }

        return photos.Select(photo => new AiImage(photo, Image(photo))).ToList();
    }

    /// <summary>Checks the recipe has room for one more photo.</summary>
    public static void RoomForImage(Recipe recipe)
    {
        if (recipe.Images.Count >= MaxImagesPerRecipe)
        {
            throw new RuleViolationException($"A recipe can have up to {MaxImagesPerRecipe} photos. Remove one before adding another.");
        }
    }

    /// <summary>The recipe's photo with that id.</summary>
    public static RecipeImage ImageOn(Recipe recipe, Guid imageId) =>
        recipe.Images.FirstOrDefault(image => image.Id == imageId)
            ?? throw new NotFoundException($"{recipe.Name} has no photo with id {imageId}.");
}
