namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One part of a creative meal.</summary>
/// <param name="Name">The ingredient.</param>
/// <param name="Group">Its role: whole grain, protein, vegetable or fruit.</param>
/// <param name="OnHand">False when it would have to be bought.</param>
/// <param name="ExpiresOn">When the pantry's stock of it expires, if known.</param>
public sealed record CreativeComponent(string Name, IngredientGroup Group, bool OnHand, DateOnly? ExpiresOn);
