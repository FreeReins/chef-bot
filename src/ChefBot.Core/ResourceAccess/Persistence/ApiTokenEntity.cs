namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class ApiTokenEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
