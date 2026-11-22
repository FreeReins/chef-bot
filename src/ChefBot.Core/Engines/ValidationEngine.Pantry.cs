using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <content>Checks on the pantry and things added to the shopping list.</content>
internal sealed partial class ValidationEngine
{
    public static void PantryItemName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new RuleViolationException("A pantry item needs a name.");
        }
    }

    /// <summary>Something typed in for the shopping list: it needs a name, and an amount above zero if it has one.</summary>
    public static void ShoppingRequest(NewShoppingItem request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RuleViolationException("Type what to buy, for example \"milk\".");
        }

        if (request.Quantity is <= 0m)
        {
            throw new RuleViolationException("An amount must be more than zero. Leave it empty if it doesn't matter.");
        }
    }

    public static void Stock(decimal? quantity, DateOnly purchasedOn, DateOnly? expiresOn)
    {
        if (quantity is <= 0m)
        {
            throw new RuleViolationException("A quantity must be more than zero.");
        }

        if (expiresOn < purchasedOn)
        {
            throw new RuleViolationException("The expiration date can't be before the purchase date.");
        }
    }

    public static void QuantityLeft(decimal? quantity)
    {
        if (quantity is < 0m)
        {
            throw new RuleViolationException("A quantity can't be negative.");
        }
    }
}
