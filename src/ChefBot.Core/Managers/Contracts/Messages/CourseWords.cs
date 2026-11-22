namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// The words that sort imported recipes into courses, matched as whole words against a recipe's tags, categories
/// and tools. The first list that matches decides, in this order. In a request, a null list is set back to its default.
/// </summary>
/// <param name="Breakfast">Labels that make a recipe a breakfast.</param>
/// <param name="Snack">Labels that make a recipe a snack.</param>
/// <param name="Sweet">Labels that make a recipe a sweet.</param>
/// <param name="Drink">Labels that make a recipe a drink.</param>
/// <param name="SlowCooker">Labels, or words in the name, that mark a recipe as made in a slow cooker.</param>
/// <param name="NotAMeal">Labels for things that are never a meal on their own, such as sides and sauces.</param>
/// <param name="Main">When not empty, only recipes carrying one of these labels count as mains.</param>
public sealed record CourseWords(
    IReadOnlyList<string>? Breakfast,
    IReadOnlyList<string>? Snack,
    IReadOnlyList<string>? Sweet,
    IReadOnlyList<string>? Drink,
    IReadOnlyList<string>? SlowCooker,
    IReadOnlyList<string>? NotAMeal,
    IReadOnlyList<string>? Main);
