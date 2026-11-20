namespace ChefBot.Core.ResourceAccess.Persistence;

// Settings: one row, with Id 1.
internal sealed class SecuritySettingsEntity
{
    public int Id { get; set; } = 1;

    public int MaxFailedSignIns { get; set; }

    public int LockoutMinutes { get; set; }

    public bool RequireHttps { get; set; }
}
