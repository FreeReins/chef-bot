namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// A named group of recipes, such as "Weeknight" or "Grandma's". A recipe can be in any number of cookbooks, or none;
/// each recipe lists the ones it is in (<see cref="Recipe.CookbookIds"/>).
/// </summary>
internal sealed class Cookbook
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    /// <summary>A line or two about what the cookbook is for.</summary>
    public string? Description { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
