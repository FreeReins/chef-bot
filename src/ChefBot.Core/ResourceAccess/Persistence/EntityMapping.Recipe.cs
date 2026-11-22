using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies RecipeEntity to and from Recipe.</content>
internal static partial class EntityMapping
{
    public static Recipe ToContract(this RecipeEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Servings = entity.Servings,
        Course = entity.Course,
        IsSlowCooker = entity.IsSlowCooker,
        IsArchived = entity.IsArchived,
        UpdatedAt = entity.UpdatedAt,
        Description = entity.Description,
        Instructions = [.. entity.Instructions],
        PrepMinutes = entity.PrepMinutes,
        CookMinutes = entity.CookMinutes,
        TotalMinutes = entity.TotalMinutes,
        SourceUrl = entity.SourceUrl,
        Author = entity.Author,
        YieldText = entity.YieldText,
        Notes = entity.Notes,
        Tags = [.. entity.Tags],
        CookbookIds = [.. entity.CookbookIds],
        Ingredients = entity.Ingredients.OrderBy(i => i.Position).Select(i => new RecipeIngredient
        {
            Id = i.Id,
            Position = i.Position,
            Key = i.Key,
            CatalogName = i.CatalogName,
            DisplayName = i.DisplayName,
            Quantity = i.Quantity,
            Unit = i.Unit,
            Text = i.Text,
            Note = i.Note,
        }).ToList(),
        Images = entity.Images.OrderBy(i => i.Position).Select(i => new RecipeImage
        {
            Id = i.Id,
            Position = i.Position,
            SourceUrl = i.SourceUrl,
            ContentType = i.ContentType,
        }).ToList(),
    };

    public static RecipeEntity ToEntity(this Recipe recipe) => new()
    {
        Id = recipe.Id,
        Name = recipe.Name,
        Servings = recipe.Servings,
        Course = recipe.Course,
        IsSlowCooker = recipe.IsSlowCooker,
        IsArchived = recipe.IsArchived,
        UpdatedAt = recipe.UpdatedAt,
        Description = recipe.Description,
        Instructions = [.. recipe.Instructions],
        PrepMinutes = recipe.PrepMinutes,
        CookMinutes = recipe.CookMinutes,
        TotalMinutes = recipe.TotalMinutes,
        SourceUrl = recipe.SourceUrl,
        Author = recipe.Author,
        YieldText = recipe.YieldText,
        Notes = recipe.Notes,
        Tags = [.. recipe.Tags],
        CookbookIds = [.. recipe.CookbookIds],
        Ingredients = recipe.Ingredients.Select(i => new RecipeIngredientEntity
        {
            Id = i.Id,
            RecipeId = recipe.Id,
            Position = i.Position,
            Key = i.Key,
            CatalogName = i.CatalogName,
            DisplayName = i.DisplayName,
            Quantity = i.Quantity,
            Unit = i.Unit,
            Text = i.Text,
            Note = i.Note,
        }).ToList(),
        Images = recipe.Images.Select(i => new RecipeImageEntity
        {
            Id = i.Id,
            RecipeId = recipe.Id,
            Position = i.Position,
            SourceUrl = i.SourceUrl,
            ContentType = i.ContentType,
        }).ToList(),
    };
}
