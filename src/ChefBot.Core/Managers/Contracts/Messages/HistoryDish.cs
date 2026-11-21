namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A dish named in a <see cref="HistoryMoment"/>: one cooked that week, or one that arrived in a stampede.</summary>
/// <param name="Title">The dish's name as it was then.</param>
/// <param name="RecipeId">Its recipe, when it is still in the library; null for archived recipes and free-form meals.</param>
/// <param name="Date">When it was cooked or added.</param>
public sealed record HistoryDish(string Title, Guid? RecipeId, DateOnly Date);
