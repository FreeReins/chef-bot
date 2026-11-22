using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>
/// Writes the food history from the events that make it, one at a time, so the moments go in in the order they
/// happened. Recording an event twice adds nothing.
/// </summary>
[StickyHandler(MessageQueues.History)]
public static class HistoryProjectionHandler
{
    public static Task Handle(MealCooked message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RecordHistoryAsync(message, envelope.Id, cancellationToken);

    public static Task Handle(MealUncooked message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RecordHistoryAsync(message, envelope.Id, cancellationToken);

    public static Task Handle(ShoppingDone message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RecordHistoryAsync(message, envelope.Id, cancellationToken);

    public static Task Handle(RecipesAdded message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RecordHistoryAsync(message, envelope.Id, cancellationToken);

    public static Task Handle(RecipeArchived message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RecordHistoryAsync(message, envelope.Id, cancellationToken);

    public static Task Handle(MemberJoined message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RecordHistoryAsync(message, envelope.Id, cancellationToken);

    public static Task Handle(KitchenOpened message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RecordHistoryAsync(message, envelope.Id, cancellationToken);
}
