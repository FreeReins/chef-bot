using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class ShoppingTargetEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ShoppingTargetKind Kind { get; set; }

    public string Url { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string List { get; set; } = string.Empty;

    public int Position { get; set; }
}
