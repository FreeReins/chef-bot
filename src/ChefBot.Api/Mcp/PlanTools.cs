using System.ComponentModel;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using ModelContextProtocol.Server;

namespace ChefBot.Api.Mcp;

/// <summary>
/// MCP tools for the weekly meal plans. Every change goes through <see cref="IPlanManager"/>, so leftovers and the
/// shopping list follow, just as they do in the web page.
/// </summary>
[McpServerToolType]
public sealed class PlanTools(IPlanManager plans)
{
    [McpServerTool(Name = "get_current_plan", ReadOnly = true, Idempotent = true)]
    [Description("The meal plan for this week, or else the next upcoming one: its meals by day, their status, leftovers and the shopping list.")]
    public Task<PlanResponse> GetCurrentPlan(CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.GetCurrentAsync(cancellationToken));

    [McpServerTool(Name = "list_open_plans", ReadOnly = true, Idempotent = true)]
    [Description("Every plan whose week hasn't ended, earliest first: this week's and any drafts for later weeks.")]
    public Task<IReadOnlyList<PlanSummaryResponse>> ListOpenPlans(CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.ListOpenAsync(cancellationToken));

    [McpServerTool(Name = "get_plan", ReadOnly = true, Idempotent = true)]
    [Description("One meal plan in full, by its id.")]
    public Task<PlanResponse> GetPlan(
        [Description("The plan's id, from list_open_plans.")] Guid planId,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.GetAsync(planId, cancellationToken));

    [McpServerTool(Name = "stage_plan")]
    [Description("Proposes a new plan for a week and saves it as a draft to review. Confirm it with confirm_plan to start shopping for it.")]
    public Task<PlanResponse> StagePlan(
        [Description("The Sunday the week starts on (yyyy-MM-dd). Leave empty for the upcoming week.")] DateOnly? weekStart = null,
        [Description("Start the week over even though its plan was already confirmed.")] bool replace = false,
        [Description("Meal ids of the week's current plan to keep as they are; the rest are planned again.")] IReadOnlyList<Guid>? keepMealIds = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.StageAsync(new StagePlanRequest(weekStart, replace, Keep: keepMealIds), cancellationToken));

    [McpServerTool(Name = "confirm_plan", Idempotent = true)]
    [Description("Makes a draft plan the working plan for its week, which opens it up for shopping.")]
    public Task<PlanResponse> ConfirmPlan(
        [Description("The draft plan's id.")] Guid planId,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.ConfirmAsync(planId, cancellationToken));

    [McpServerTool(Name = "suggest_alternatives", ReadOnly = true)]
    [Description("Suggests replacements for one meal of a plan: recipes from the library that use what is in the pantry, and free-form ideas.")]
    public Task<AlternativesResponse> SuggestAlternatives(
        [Description("The plan's id.")] Guid planId,
        [Description("The meal's id.")] Guid mealId,
        [Description("How many library recipes to suggest, from 1 to 20.")] int recipeLimit = 8,
        [Description("How many free-form ideas to suggest, from 0 to 10.")] int ideaCount = 3,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.GetAlternativesAsync(
            planId, mealId, Math.Clamp(recipeLimit, 1, 20), Math.Clamp(ideaCount, 0, 10), cancellationToken));

    [McpServerTool(Name = "replace_meal")]
    [Description("Replaces a meal with a recipe from the library, or with a free-form meal given as a title and ingredients. Give either recipeId or customTitle, not both.")]
    public Task<PlanResponse> ReplaceMeal(
        [Description("The plan's id.")] Guid planId,
        [Description("The meal's id.")] Guid mealId,
        [Description("The library recipe to cook instead.")] Guid? recipeId = null,
        [Description("The name of a free-form meal to cook instead.")] string? customTitle = null,
        [Description("The free-form meal's ingredients, such as \"2 cups rice\".")] IReadOnlyList<string>? customIngredients = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.ReplaceMealAsync(
            planId,
            mealId,
            new ReplaceMealRequest(recipeId, customTitle is null ? null : new CustomMealRequest(customTitle, customIngredients ?? [])),
            cancellationToken));

    [McpServerTool(Name = "add_meal")]
    [Description("Adds a library recipe to a week's plan, or swaps it in for one of its meals, optionally for a chosen number of servings. A week with no plan yet gets a staged one. The shopping list catches up a moment later.")]
    public Task<PlanResponse> AddMeal(
        [Description("The library recipe to add.")] Guid recipeId,
        [Description("The Sunday the week starts on (yyyy-MM-dd).")] DateOnly weekStart,
        [Description("Which meal it is: Breakfast, Lunch, Dinner, Snack, Sweet or Drink.")] MealSlot slot,
        [Description("The day (yyyy-MM-dd) within the week. Leave out for a snack, sweet or drink made on any day.")] DateOnly? date = null,
        [Description("How many servings to cook, from 1 to 50. Leave out to cook for the plan's number of people.")] int? servings = null,
        [Description("The id of a meal on the plan to swap for this recipe. Leave out to add it alongside.")] Guid? replaces = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.AddMealAsync(new AddMealRequest(recipeId, weekStart, date, slot, servings, replaces), cancellationToken));

    [McpServerTool(Name = "move_meal")]
    [Description("Moves a dinner or breakfast to another day of its week. A meal of the same kind already on that day swaps places with it.")]
    public Task<PlanResponse> MoveMeal(
        [Description("The plan's id.")] Guid planId,
        [Description("The meal's id.")] Guid mealId,
        [Description("The day to move it to (yyyy-MM-dd), within the plan's week.")] DateOnly date,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.MoveMealAsync(planId, mealId, new MoveMealRequest(date), cancellationToken));

    [McpServerTool(Name = "set_meal_status", Idempotent = true)]
    [Description("Marks a meal as Planned, Cooked or Skipped. Cooked takes its ingredients out of the pantry; going back from Cooked doesn't put them back.")]
    public Task<PlanResponse> SetMealStatus(
        [Description("The plan's id.")] Guid planId,
        [Description("The meal's id.")] Guid mealId,
        [Description("Planned, Cooked or Skipped.")] MealStatus status,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => plans.SetMealStatusAsync(planId, mealId, new MealStatusRequest(status), cancellationToken));
}
