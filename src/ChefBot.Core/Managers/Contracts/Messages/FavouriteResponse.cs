namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Someone who has a recipe as one of their favourites.</summary>
/// <param name="UserId">Their account's id, to tell whether a favourite is the signed-in person's own.</param>
/// <param name="Name">Their display name.</param>
public sealed record FavouriteResponse(Guid UserId, string Name);
