using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <content>Checks on grocery stores and the to-do apps the shopping list is sent to.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>
    /// Checks a new list of stores: each has a name of its own, at most one is the main store, and a seasonal store
    /// says which months it is open.
    /// </summary>
    public static IReadOnlyList<GroceryStoreDetails> GroceryStores(IReadOnlyList<GroceryStoreDetails>? stores)
    {
        var list = stores ?? [];
        if (list.Count > MaxStores)
        {
            throw new RuleViolationException($"Keep to {MaxStores} stores or fewer.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var store in list)
        {
            var name = store.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                throw new RuleViolationException("Every store needs a name.");
            }

            if (name.Length > MaxStoreName)
            {
                throw new RuleViolationException($"Keep store names to {MaxStoreName} characters or fewer.");
            }

            if (!names.Add(name))
            {
                throw new RuleViolationException($"There are two stores called {name}. Give each store its own name.");
            }

            if ((store.Months ?? []).Any(month => month is < 1 or > 12))
            {
                throw new RuleViolationException($"{name} has a month that isn't between 1 and 12.");
            }

            if (store.SeasonalProduce && (store.Months ?? []).Count == 0)
            {
                throw new RuleViolationException($"Choose the months {name} is open, or stop sending in-season produce there.");
            }
        }

        if (list.Count(store => store.IsMain) > 1)
        {
            throw new RuleViolationException("Only one store can be the main store. Choose one and untick the others.");
        }

        return list;
    }

    /// <summary>
    /// Checks the to-do apps to save: each has its own name, an address where its kind needs one, and a token where
    /// its kind needs one (a new one, or one already saved for it in <paramref name="current"/>).
    /// </summary>
    /// <exception cref="RuleViolationException">A target is missing something, or there are too many.</exception>
    public static IReadOnlyList<ShoppingTargetDraft> ShoppingTargets(IReadOnlyList<ShoppingTargetDraft>? targets, IReadOnlyList<ShoppingTarget> current)
    {
        var list = targets ?? [];
        if (list.Count > MaxShoppingTargets)
        {
            throw new RuleViolationException($"Keep to {MaxShoppingTargets} to-do apps or fewer.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in list)
        {
            var name = target.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                throw new RuleViolationException("Every to-do app needs a name, such as Kitchen list.");
            }

            if (name.Length > MaxStoreName)
            {
                throw new RuleViolationException($"Keep to-do app names to {MaxStoreName} characters or fewer.");
            }

            if (!names.Add(name))
            {
                throw new RuleViolationException($"There are two to-do apps called {name}. Give each its own name.");
            }

            if (!Enum.IsDefined(target.Kind))
            {
                throw new RuleViolationException($"Choose what kind of app {name} is: Home Assistant, Todoist or a webhook.");
            }

            var url = target.Url?.Trim() ?? string.Empty;
            var hasToken = target.Token is { } token
                ? token.Trim().Length > 0
                : current.Any(saved => saved.Id == target.Id && saved.Token.Length > 0);
            switch (target.Kind)
            {
                case ShoppingTargetKind.HomeAssistant when !WebAddress.IsHttp(url):
                    throw new RuleViolationException($"Enter the address {name} is reached at, such as http://homeassistant.local:8123.");
                case ShoppingTargetKind.HomeAssistant when !(target.List?.Trim() ?? string.Empty).StartsWith("todo.", StringComparison.Ordinal):
                    throw new RuleViolationException($"Enter the to-do list for {name} as Home Assistant names it, such as todo.shopping_list.");
                case ShoppingTargetKind.HomeAssistant or ShoppingTargetKind.Todoist when !hasToken:
                    throw new RuleViolationException($"Enter an access token for {name}. {TokenHelp(target.Kind)}");
                case ShoppingTargetKind.Webhook when !WebAddress.IsHttp(url):
                    throw new RuleViolationException($"Enter the webhook address for {name}: a full http or https address.");
            }
        }

        return list;
    }

    /// <summary>One of the saved to-do apps.</summary>
    /// <exception cref="NotFoundException">There is no saved to-do app with that id.</exception>
    public static ShoppingTarget ShoppingTarget(IReadOnlyList<ShoppingTarget> targets, Guid id) =>
        targets.FirstOrDefault(target => target.Id == id)
        ?? throw new NotFoundException("That to-do app isn't set up any more. Reload the page and choose another.");

    private static string TokenHelp(ShoppingTargetKind kind) => kind == ShoppingTargetKind.HomeAssistant
        ? "Make a long-lived access token on your Home Assistant profile page, under Security."
        : "Copy your API token from Todoist's settings, under Integrations, then Developer.";

    /// <summary>The household's store with this name, or null for an empty name, meaning no particular store.</summary>
    public static GroceryStore? StoreNamed(IReadOnlyList<GroceryStore> stores, string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? null
            : stores.FirstOrDefault(store => string.Equals(store.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
              ?? throw new NotFoundException($"There is no store called {name.Trim()}. Add it on the Settings tab first.");
}
