using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>
/// Looks for newly earned achievements after anything that can earn one. The check works everything out again from
/// what is saved, so it doesn't matter which event asked, or whether one arrives twice; one at a time, so a tier is
/// never saved twice.
/// </summary>
[StickyHandler(MessageQueues.Achievements)]
public static class AchievementProgressHandler
{
    public static Task Handle(CheckAchievements message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.CheckAchievementsAsync(envelope.Id, cancellationToken);

    public static Task Handle(RecipesAdded message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.CheckAchievementsAsync(envelope.Id, cancellationToken);

    public static Task Handle(RecipeFavourited message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.CheckAchievementsAsync(envelope.Id, cancellationToken);

    public static Task Handle(RecipeReviewed message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.CheckAchievementsAsync(envelope.Id, cancellationToken);

    public static Task Handle(PlanStaged message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.CheckAchievementsAsync(envelope.Id, cancellationToken);

    public static Task Handle(PlanConfirmed message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.CheckAchievementsAsync(envelope.Id, cancellationToken);

    // Cooks and shops count from the food history, so they are looked at once it has them.
    public static Task Handle(HistoryRecorded message, Envelope envelope, IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.CheckAchievementsAsync(envelope.Id, cancellationToken);
}
