using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>The household's grocery stores and plan styles.</content>
internal sealed partial class PlanManager
{
    public async Task<GroceryStoresResponse> GetStoresAsync(CancellationToken cancellationToken) =>
        DescribeStores(await settings.ListStoresAsync(cancellationToken));

    public async Task<GroceryStoresResponse> SaveStoresAsync(SaveGroceryStoresRequest request, CancellationToken cancellationToken)
    {
        var details = ValidationEngine.GroceryStores(request.Stores);
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var stores = details.ToStores(await settings.ListStoresAsync(cancellationToken));

        await settings.SaveStoresAsync(stores, cancellationToken);
        await messages.PublishAsync(new GroceryStoresChanged()); // the open plans follow in the background
        await messages.CommitAsync(cancellationToken);
        return DescribeStores(stores);
    }

    public async Task<PlanStylesResponse> GetStylesAsync(CancellationToken cancellationToken) =>
        await DescribeStylesAsync(await settings.ListPlanStylesAsync(cancellationToken), cancellationToken);

    public async Task<PlanStylesResponse> SaveStylesAsync(SavePlanStylesRequest request, CancellationToken cancellationToken)
    {
        var details = ValidationEngine.PlanStyles(request.Styles);
        await validation.CookbookRulesAsync(details, cancellationToken);
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var styles = details.ToStyles(await settings.ListPlanStylesAsync(cancellationToken));

        await settings.SavePlanStylesAsync(styles, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await DescribeStylesAsync(styles, cancellationToken);
    }

    public async Task<int> ForgetCookbookAsync(Guid cookbookId, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var styles = await settings.ListPlanStylesAsync(cancellationToken);
        var id = cookbookId.ToString();
        var changed = 0;
        var dropped = 0;
        foreach (var style in styles)
        {
            foreach (var rule in style.Rules.Where(rule => rule.Match == RecipeMatchKind.Cookbook))
            {
                changed += rule.Values.RemoveAll(value => string.Equals(value, id, StringComparison.OrdinalIgnoreCase));
            }

            dropped += style.Rules.RemoveAll(rule => rule.Match == RecipeMatchKind.Cookbook && rule.Values.Count == 0);
        }

        if (changed > 0)
        {
            await settings.SavePlanStylesAsync(styles, cancellationToken);
            await messages.CommitAsync(cancellationToken);
        }

        return dropped;
    }

    /// <summary>The style with this id, or else the household's default: its default style, then the first starter style.</summary>
    private async Task<PlanStyle> StyleAsync(Guid? id, CancellationToken cancellationToken)
    {
        var saved = await settings.ListPlanStylesAsync(cancellationToken);
        return ValidationEngine.StyleWithId(saved, knownStyles.All(), id) ?? DefaultStyle(saved);
    }

    /// <summary>The saved style with this name, as when a plan is made again; the default style when it has gone.</summary>
    private async Task<PlanStyle> StyleNamedAsync(string name, CancellationToken cancellationToken)
    {
        var saved = await settings.ListPlanStylesAsync(cancellationToken);
        return saved.Concat(knownStyles.All()).FirstOrDefault(style => string.Equals(style.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? DefaultStyle(saved);
    }

    private PlanStyle DefaultStyle(IReadOnlyList<PlanStyle> saved) =>
        saved.FirstOrDefault(style => style.IsDefault) ?? saved.FirstOrDefault() ?? knownStyles.All().FirstOrDefault() ?? PlanStyle.Classic();

    private async Task<PlanStylesResponse> DescribeStylesAsync(IReadOnlyList<PlanStyle> styles, CancellationToken cancellationToken)
    {
        var tags = (await recipes.ListActiveTagsAsync(cancellationToken))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new PlanStylesResponse(
            styles.Select(style => style.ToResponse()).ToList(),
            knownStyles.All().Select(style => style.ToResponse(saved: false)).ToList(),
            tags,
            catalog.Entries.Where(entry => !entry.PantryStaple).Select(entry => entry.Name).Order(StringComparer.Ordinal).ToList(),
            (await cookbooks.ListAsync(cancellationToken)).Select(cookbook => new RecipeCookbookResponse(cookbook.Id, cookbook.Name)).ToList());
    }

    private GroceryStoresResponse DescribeStores(IReadOnlyList<GroceryStore> stores) => new(
        stores.Select(store => store.ToResponse()).ToList(),
        knownStores.All().Select(store => store.ToResponse()).ToList(),
        catalog.Entries.Where(entry => !entry.PantryStaple).Select(entry => entry.Name).Order(StringComparer.Ordinal).ToList());
}
