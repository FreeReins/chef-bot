using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>The household's reviews of recipes. Reviews outlive the people who wrote them and archived recipes.</summary>
internal interface IReviewAccess
{
    /// <summary>One recipe's reviews, newest first.</summary>
    Task<List<RecipeReview>> ListForRecipeAsync(Guid recipeId, CancellationToken cancellationToken);

    /// <summary>Every review, oldest first.</summary>
    Task<List<RecipeReview>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>How many reviews have been written, by everyone.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>The reviews of these recipes added up, by recipe. Recipes with no reviews are left out.</summary>
    Task<Dictionary<Guid, ReviewTally>> TallyAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken);

    /// <summary>One review, or null when there is none with that id.</summary>
    Task<RecipeReview?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(RecipeReview review, CancellationToken cancellationToken);

    /// <summary>Saves new stars and notes on a review that is already stored.</summary>
    Task UpdateAsync(RecipeReview review, CancellationToken cancellationToken);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
}
