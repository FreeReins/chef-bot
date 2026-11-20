using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>
/// A read-only view of the pantry on one day. Expired items are left out,
/// so planning and shopping never count on food that has gone off.
/// </summary>
internal sealed class PantrySnapshot
{
    private readonly ILookup<string, PantryItem> _byKey;
    private readonly Dictionary<string, DateOnly?> _earliestExpiry = new(StringComparer.Ordinal);
    private readonly DateOnly _today;
    private readonly int _expiringSoonDays;

    public PantrySnapshot(IEnumerable<PantryItem> items, DateOnly today, int expiringSoonDays)
    {
        _today = today;
        _expiringSoonDays = expiringSoonDays;
        Usable = items.Where(item => !item.IsExpired(today)).ToList();
        _byKey = Usable.ToLookup(item => item.Key, StringComparer.Ordinal);

        foreach (var item in Usable)
        {
            var name = item.MatchName();
            if (!_earliestExpiry.TryGetValue(name, out var known) || IsEarlier(item.ExpiresOn, known))
            {
                _earliestExpiry[name] = item.ExpiresOn;
            }
        }
    }

    public static PantrySnapshot Empty { get; } = new PantrySnapshot([], DateOnly.MinValue, 0);

    /// <summary>Everything that hasn't expired.</summary>
    public IReadOnlyList<PantryItem> Usable { get; }

    /// <summary>True when something matching this name is on hand. See <see cref="IngredientRefExtensions.MatchName"/>.</summary>
    public bool Has(string matchName) => _earliestExpiry.ContainsKey(matchName);

    /// <summary>The soonest expiration date among matching items, if any has one.</summary>
    public DateOnly? ExpiryOf(string matchName) => _earliestExpiry.GetValueOrDefault(matchName);

    public int? DaysLeft(string matchName) =>
        ExpiryOf(matchName) is DateOnly date ? date.DayNumber - _today.DayNumber : null;

    public bool ExpiresSoon(string matchName) => DaysLeft(matchName) is int days && days <= _expiringSoonDays;

    /// <summary>Usable items with exactly this key, used when amounts matter.</summary>
    public IReadOnlyList<PantryItem> WithKey(string key) => _byKey[key].ToList();

    private static bool IsEarlier(DateOnly? candidate, DateOnly? known) =>
        candidate is DateOnly date && (known is not DateOnly existing || date < existing);
}
