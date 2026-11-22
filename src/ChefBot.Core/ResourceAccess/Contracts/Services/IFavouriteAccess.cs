using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>The recipes each person has marked as a favourite.</summary>
internal interface IFavouriteAccess
{
    /// <summary>The favourites of these recipes, each with the person's name, oldest first.</summary>
    Task<List<RecipeFavourite>> ListForRecipesAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken);

    /// <summary>Every favourite, each with the person's name, oldest first.</summary>
    Task<List<RecipeFavourite>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>How many favourites are marked, by people who still have an account.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>Everyone with a favourite, in the order they marked their first.</summary>
    Task<List<FavouritePerson>> ListPeopleAsync(CancellationToken cancellationToken);

    /// <summary>One person's favourite of one recipe, or null when they haven't marked it.</summary>
    Task<RecipeFavourite?> FindAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken);

    Task AddAsync(RecipeFavourite favourite, CancellationToken cancellationToken);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Removes every favourite of a person, when they leave the household.</summary>
    Task RemoveForUserAsync(Guid userId, CancellationToken cancellationToken);
}
