namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class CookbookEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
