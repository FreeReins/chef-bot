namespace ChefBot.Core.ResourceAccess.Persistence;

// Settings: one row, with Id 1.
internal sealed class AdvancedSettingsEntity
{
    public int Id { get; set; } = 1;

    public int AvoidRepeatDays { get; set; }

    public int ExpiringSoonDays { get; set; }

    public int PerishableWithinDays { get; set; }

    public List<string> BreakfastWords { get; set; } = [];

    public List<string> SnackWords { get; set; } = [];

    public List<string> SweetWords { get; set; } = [];

    public List<string> DrinkWords { get; set; } = [];

    public List<string> SlowCookerWords { get; set; } = [];

    public List<string> NotAMealWords { get; set; } = [];

    public List<string> MainWords { get; set; } = [];
}
