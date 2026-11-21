using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <summary>Copies between the stored entities and the data contracts, in both directions.</summary>
internal static partial class EntityMapping
{
    // ---------- entity to contract ----------

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

    public static MealPlan ToContract(this MealPlanEntity entity)
    {
        // Meals that follow the same recipe share one copy of it, as they share one row.
        var recipes = entity.Meals
            .Where(meal => meal.Recipe is not null)
            .Select(meal => meal.Recipe!)
            .DistinctBy(recipe => recipe.Id)
            .ToDictionary(recipe => recipe.Id, recipe => recipe.ToContract());

        return new MealPlan
        {
            Id = entity.Id,
            WeekStart = entity.WeekStart,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            StyleName = entity.StyleName,
            LeftoverLunches = entity.LeftoverLunches,
            Meals = entity.Meals.Select(meal => new PlannedMeal
            {
                Id = meal.Id,
                Date = meal.Date,
                Slot = meal.Slot,
                RecipeId = meal.RecipeId,
                Recipe = meal.RecipeId is Guid id ? recipes.GetValueOrDefault(id) : null,
                Title = meal.Title,
                Portions = meal.Portions,
                IsOptional = meal.IsOptional,
                LeftoverOfMealId = meal.LeftoverOfMealId,
                Status = meal.Status,
                CustomIngredients = meal.CustomIngredients.Select(i => new PlannedMealIngredient
                {
                    Id = i.Id,
                    Key = i.Key,
                    CatalogName = i.CatalogName,
                    DisplayName = i.DisplayName,
                }).ToList(),
            }).ToList(),
            ShoppingItems = entity.ShoppingItems.Select(item => new ShoppingItem
            {
                Id = item.Id,
                Key = item.Key,
                CatalogName = item.CatalogName,
                DisplayName = item.DisplayName,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Store = item.Store,
                IsOptional = item.IsOptional,
                IsPurchased = item.IsPurchased,
                PurchasedOn = item.PurchasedOn,
            }).ToList(),
        };
    }

    public static PantryItem ToContract(this PantryItemEntity entity) => new()
    {
        Id = entity.Id,
        Key = entity.Key,
        CatalogName = entity.CatalogName,
        DisplayName = entity.DisplayName,
        Quantity = entity.Quantity,
        Unit = entity.Unit,
        PurchasedOn = entity.PurchasedOn,
        ExpiresOn = entity.ExpiresOn,
    };

    public static ManualShoppingItem ToContract(this ManualShoppingItemEntity entity) => new()
    {
        Id = entity.Id,
        Key = entity.Key,
        CatalogName = entity.CatalogName,
        DisplayName = entity.DisplayName,
        Quantity = entity.Quantity,
        Unit = entity.Unit,
        AddedOn = entity.AddedOn,
    };

    public static PlanStyle ToContract(this PlanStyleEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        IsDefault = entity.IsDefault,
        Position = entity.Position,
        CookingDays = [.. entity.CookingDays],
        LeftoverLunches = entity.LeftoverLunches,
        Breakfasts = entity.Breakfasts,
        Snacks = entity.Snacks,
        Sweets = entity.Sweets,
        Drinks = entity.Drinks,
        Rules = JsonSerializer.Deserialize(entity.Rules, RuleJson.Default.ListPlanRule) ?? [],
    };

    public static GroceryStore ToContract(this GroceryStoreEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        IsMain = entity.IsMain,
        Position = entity.Position,
        Items = [.. entity.Items],
        SeasonalProduce = entity.SeasonalProduce,
        Months = [.. entity.Months],
    };

    public static ShoppingTarget ToContract(this ShoppingTargetEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Kind = entity.Kind,
        Url = entity.Url,
        Token = entity.Token,
        List = entity.List,
        Position = entity.Position,
    };

    public static UserAccount ToContract(this UserEntity entity) => new()
    {
        Id = entity.Id,
        Username = entity.Username,
        DisplayName = entity.DisplayName,
        PasswordHash = entity.PasswordHash,
        IsAdmin = entity.IsAdmin,
        CreatedAt = entity.CreatedAt,
        FailedSignIns = entity.FailedSignIns,
        LockedUntil = entity.LockedUntil,
        SessionVersion = entity.SessionVersion,
    };

    public static AccessLink ToContract(this AccessLinkEntity entity) => new()
    {
        Id = entity.Id,
        Purpose = entity.Purpose,
        TokenHash = entity.TokenHash,
        UserId = entity.UserId,
        CreatedByUserId = entity.CreatedByUserId,
        CreatedAt = entity.CreatedAt,
        ExpiresAt = entity.ExpiresAt,
    };

    public static ApiToken ToContract(this ApiTokenEntity entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        Name = entity.Name,
        TokenHash = entity.TokenHash,
        CreatedAt = entity.CreatedAt,
    };

    public static FoodMoment ToContract(this FoodMomentEntity entity) => new()
    {
        Id = entity.Id,
        OccurredOn = entity.OccurredOn,
        Kind = entity.Kind,
        RecipeId = entity.RecipeId,
        MealId = entity.MealId,
        Slot = entity.Slot,
        Title = entity.Title,
        Count = entity.Count,
    };

    // ---------- contract to entity ----------

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

    public static MealPlanEntity ToEntity(this MealPlan plan) => new()
    {
        Id = plan.Id,
        WeekStart = plan.WeekStart,
        Status = plan.Status,
        CreatedAt = plan.CreatedAt,
        StyleName = plan.StyleName,
        LeftoverLunches = plan.LeftoverLunches,
        Meals = plan.Meals.Select(meal => new PlannedMealEntity
        {
            Id = meal.Id,
            MealPlanId = plan.Id,
            Date = meal.Date,
            Slot = meal.Slot,
            RecipeId = meal.RecipeId,
            Title = meal.Title,
            Portions = meal.Portions,
            IsOptional = meal.IsOptional,
            LeftoverOfMealId = meal.LeftoverOfMealId,
            Status = meal.Status,
            CustomIngredients = meal.CustomIngredients.Select(i => new PlannedMealIngredientEntity
            {
                Id = i.Id,
                PlannedMealId = meal.Id,
                Key = i.Key,
                CatalogName = i.CatalogName,
                DisplayName = i.DisplayName,
            }).ToList(),
        }).ToList(),
        ShoppingItems = plan.ShoppingItems.Select(item => new ShoppingItemEntity
        {
            Id = item.Id,
            MealPlanId = plan.Id,
            Key = item.Key,
            CatalogName = item.CatalogName,
            DisplayName = item.DisplayName,
            Quantity = item.Quantity,
            Unit = item.Unit,
            Store = item.Store,
            IsOptional = item.IsOptional,
            IsPurchased = item.IsPurchased,
            PurchasedOn = item.PurchasedOn,
        }).ToList(),
    };

    public static PantryItemEntity ToEntity(this PantryItem item) => new()
    {
        Id = item.Id,
        Key = item.Key,
        CatalogName = item.CatalogName,
        DisplayName = item.DisplayName,
        Quantity = item.Quantity,
        Unit = item.Unit,
        PurchasedOn = item.PurchasedOn,
        ExpiresOn = item.ExpiresOn,
    };

    public static ManualShoppingItemEntity ToEntity(this ManualShoppingItem item) => new()
    {
        Id = item.Id,
        Key = item.Key,
        CatalogName = item.CatalogName,
        DisplayName = item.DisplayName,
        Quantity = item.Quantity,
        Unit = item.Unit,
        AddedOn = item.AddedOn,
    };

    public static GroceryStoreEntity ToEntity(this GroceryStore store) => new()
    {
        Id = store.Id,
        Name = store.Name,
        IsMain = store.IsMain,
        Position = store.Position,
        Items = [.. store.Items],
        SeasonalProduce = store.SeasonalProduce,
        Months = [.. store.Months],
    };

    public static ShoppingTargetEntity ToEntity(this ShoppingTarget target) => new()
    {
        Id = target.Id,
        Name = target.Name,
        Kind = target.Kind,
        Url = target.Url,
        Token = target.Token,
        List = target.List,
        Position = target.Position,
    };

    public static PlanStyleEntity ToEntity(this PlanStyle style) => new()
    {
        Id = style.Id,
        Name = style.Name,
        Description = style.Description,
        IsDefault = style.IsDefault,
        Position = style.Position,
        CookingDays = [.. style.CookingDays],
        LeftoverLunches = style.LeftoverLunches,
        Breakfasts = style.Breakfasts,
        Snacks = style.Snacks,
        Sweets = style.Sweets,
        Drinks = style.Drinks,
        Rules = JsonSerializer.Serialize(style.Rules, RuleJson.Default.ListPlanRule),
    };

    public static UserEntity ToEntity(this UserAccount user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        DisplayName = user.DisplayName,
        PasswordHash = user.PasswordHash,
        IsAdmin = user.IsAdmin,
        CreatedAt = user.CreatedAt,
        FailedSignIns = user.FailedSignIns,
        LockedUntil = user.LockedUntil,
        SessionVersion = user.SessionVersion,
    };

    public static AccessLinkEntity ToEntity(this AccessLink link) => new()
    {
        Id = link.Id,
        Purpose = link.Purpose,
        TokenHash = link.TokenHash,
        UserId = link.UserId,
        CreatedByUserId = link.CreatedByUserId,
        CreatedAt = link.CreatedAt,
        ExpiresAt = link.ExpiresAt,
    };

    public static ApiTokenEntity ToEntity(this ApiToken token) => new()
    {
        Id = token.Id,
        UserId = token.UserId,
        Name = token.Name,
        TokenHash = token.TokenHash,
        CreatedAt = token.CreatedAt,
    };

    public static FoodMomentEntity ToEntity(this FoodMoment moment) => new()
    {
        Id = moment.Id,
        OccurredOn = moment.OccurredOn,
        Kind = moment.Kind,
        RecipeId = moment.RecipeId,
        MealId = moment.MealId,
        Slot = moment.Slot,
        Title = moment.Title,
        Count = moment.Count,
    };

    // ---------- saving ----------

    /// <summary>
    /// Brings a tracked row in line with a freshly mapped one: copies its columns, then does the same for each
    /// child collection, adding new children and deleting ones that are gone.
    /// </summary>
    public static void CopyInto<TEntity>(this DbContext db, TEntity tracked, TEntity fresh)
        where TEntity : class =>
        db.Entry(tracked).CurrentValues.SetValues(fresh);

    /// <summary>Makes <paramref name="current"/> hold exactly the children in <paramref name="fresh"/>, matched by id.</summary>
    /// <param name="db">The context tracking <paramref name="current"/>.</param>
    /// <param name="current">The tracked collection.</param>
    /// <param name="fresh">The children as they should be.</param>
    /// <param name="id">Reads a child's id.</param>
    /// <param name="nested">Syncs a matched child's own children, if it has any.</param>
    public static void SyncChildren<TEntity>(
        this DbContext db, List<TEntity> current, IEnumerable<TEntity> fresh, Func<TEntity, Guid> id, Action<TEntity, TEntity>? nested = null)
        where TEntity : class
    {
        var wanted = fresh.ToList();
        var wantedIds = wanted.Select(id).ToHashSet();

        // Removing a child from its parent's collection deletes its row.
        current.RemoveAll(child => !wantedIds.Contains(id(child)));

        var existing = current.ToDictionary(id);
        foreach (var child in wanted)
        {
            if (existing.TryGetValue(id(child), out var match))
            {
                db.CopyInto(match, child);
                nested?.Invoke(match, child);
            }
            else
            {
                current.Add(child);
            }
        }
    }

    /// <summary>The JSON shape of a plan style's rules, as stored in its row, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(List<PlanRule>))]
    private sealed partial class RuleJson : JsonSerializerContext;
}
