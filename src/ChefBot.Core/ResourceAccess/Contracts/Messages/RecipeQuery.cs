using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>Which recipes that can be planned to find, and which slice of them, in course order and then by name.</summary>
/// <param name="Words">Each must appear, in any case, in the name, an ingredient line or a tag.</param>
/// <param name="Course">Only this course; null for all.</param>
/// <param name="Skip">How many matches to pass over.</param>
/// <param name="Take">How many matches to return at most.</param>
/// <param name="Tags">Each must be one of the recipe's tags, in any case.</param>
/// <param name="FavouritesOnly">True for only recipes someone has as a favourite.</param>
/// <param name="FavouredBy">Only recipes this person has as a favourite; null for anyone's.</param>
/// <param name="SlowCooker">True for only slow cooker recipes.</param>
/// <param name="ReadyWithin">Only recipes whose total time, or else prep plus cook time, is at most this many minutes.</param>
internal sealed record RecipeQuery(
    IReadOnlyList<string> Words,
    RecipeCourse? Course,
    int Skip,
    int Take,
    IReadOnlyList<string>? Tags = null,
    bool FavouritesOnly = false,
    Guid? FavouredBy = null,
    bool SlowCooker = false,
    int? ReadyWithin = null);
