using ChefBot.Core.Managers.Contracts;

namespace ChefBot.Core.Utilities;

/// <summary>
/// How choices are scored, and the servings assumed for imported recipes. Bound from the "Planner" settings section.
/// What a week holds comes from the household's plan styles, and how many people it is for, the weekly schedule and the
/// day ranges from the settings saved in the app.
/// </summary>
internal sealed class PlannerOptions
{
    /// <summary>Used when an imported recipe doesn't say how many it serves.</summary>
    public decimal DefaultServings { get; set; } = 4;

    public ScoreWeights Weights { get; set; } = new();

    /// <summary>Fails fast on settings the planner can't work with.</summary>
    public void Validate()
    {
        if (DefaultServings <= 0)
        {
            throw new RuleViolationException("Planner DefaultServings must be positive.");
        }
    }
}
