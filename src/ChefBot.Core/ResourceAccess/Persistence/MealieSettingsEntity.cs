namespace ChefBot.Core.ResourceAccess.Persistence;

// Settings: one row, with Id 1.
internal sealed class MealieSettingsEntity
{
    public int Id { get; set; } = 1;

    public string Url { get; set; } = string.Empty;

    public string ApiToken { get; set; } = string.Empty;
}
