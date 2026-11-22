namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>Someone who has marked at least one favourite, by the name they have now.</summary>
internal sealed record FavouritePerson(Guid UserId, string Name);
