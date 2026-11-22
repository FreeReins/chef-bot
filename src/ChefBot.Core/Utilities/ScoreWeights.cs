namespace ChefBot.Core.Utilities;

/// <summary>Points added to or taken from a candidate week. Positive is better.</summary>
internal sealed class ScoreWeights
{
    /// <summary>Per extra meal that uses the same perishable ingredient.</summary>
    public double SharedPerishable { get; set; } = 3;

    /// <summary>A perishable only one meal uses, and that isn't already in the pantry.</summary>
    public double StrandedPerishable { get; set; } = -1;

    /// <summary>A recipe whose seasonal produce is all in season scores this; all out of season scores the negative.</summary>
    public double Seasonal { get; set; } = 3;

    /// <summary>Per ingredient already in the pantry.</summary>
    public double OnHand { get; set; } = 1.5;

    /// <summary>Extra, per pantry ingredient that is about to expire.</summary>
    public double ExpiringSoon { get; set; } = 4;

    /// <summary>Per meal beyond two built on the same protein.</summary>
    public double SameProtein { get; set; } = -3;

    /// <summary>How much more pantry food counts when a plan is rebuilt around what was already bought.</summary>
    public double RegeneratePantryBoost { get; set; } = 4;

    /// <summary>
    /// A favourite recipe, for a plan style that leans on favourites a little. Each further person who has it as a
    /// favourite adds less: half as much for the second, a third for the third, and so on.
    /// </summary>
    public double Favourite { get; set; } = 2;

    /// <summary>The same, for a plan style that leans on favourites a lot.</summary>
    public double FavouriteALot { get; set; } = 6;

    /// <summary>Per ingredient a swap suggestion would add to the shopping list.</summary>
    public double MissingIngredient { get; set; } = -0.5;
}
