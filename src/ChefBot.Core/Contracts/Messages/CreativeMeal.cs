namespace ChefBot.Core.Contracts.Messages;

/// <summary>A meal idea built from a whole grain, a protein and a vegetable or fruit.</summary>
public sealed record CreativeMeal(string Title, IReadOnlyList<CreativeComponent> Components)
{
    /// <summary>The ingredient names, ready to send back when choosing this idea.</summary>
    public IReadOnlyList<string> Ingredients => Components.Select(component => component.Name).ToList();
}
