using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class PlanStyleEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public int Position { get; set; }

    public List<DayOfWeek> CookingDays { get; set; } = [];

    public bool LeftoverLunches { get; set; }

    public int Breakfasts { get; set; }

    public int Snacks { get; set; }

    public int Sweets { get; set; }

    public int Drinks { get; set; }

    public FavouriteLean Favourites { get; set; } = FavouriteLean.ALittle;

    /// <summary>The style's rules as JSON. They are only ever read and saved with their style.</summary>
    public string Rules { get; set; } = "[]";
}
