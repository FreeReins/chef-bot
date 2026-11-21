using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.ResourceAccess;

/// <summary>The starter plan styles, read once from config/plan-styles.json.</summary>
internal sealed partial class KnownPlanStyleAccess(string path) : IKnownPlanStyleAccess
{
    private readonly Lazy<IReadOnlyList<PlanStyle>> _all = new(() => Load(path));

    public IReadOnlyList<PlanStyle> All() => _all.Value;

    /// <summary>
    /// Reads a file shaped like <c>{ "styles": [ { "name": "Classic week", "cookingDays": ["Sunday", ...], ... } ] }</c>.
    /// The styles keep their order and the first is the default.
    /// </summary>
    public static IReadOnlyList<PlanStyle> FromJson(string json) =>
        (JsonSerializer.Deserialize(json, StyleJson.Default.StyleFile)?.Styles ?? [])
            .Select((entry, index) => new PlanStyle
            {
                Name = entry.Name,
                Description = entry.Description,
                IsDefault = index == 0,
                Position = index,
                CookingDays = [.. entry.CookingDays],
                LeftoverLunches = entry.LeftoverLunches ?? true,
                Breakfasts = entry.Breakfasts,
                Snacks = entry.Snacks,
                Sweets = entry.Sweets,
                Drinks = entry.Drinks,
                Rules = entry.Rules.Select(rule => new PlanRule
                {
                    Limit = rule.Limit,
                    Count = rule.Count,
                    Slot = rule.Slot,
                    Match = rule.Match,
                    Values = [.. rule.Values],
                    Minutes = rule.Minutes,
                    Day = rule.Day,
                }).ToList(),
            })
            .ToList();

    private static IReadOnlyList<PlanStyle> Load(string path) => File.Exists(path) ? FromJson(File.ReadAllText(path)) : [];

    /// <summary>The whole file.</summary>
    internal sealed record StyleFile(List<StyleEntry>? Styles);

    /// <summary>
    /// One starter style as the file writes it. The lists turn a missing or null value into an empty list: the
    /// build-time JSON reader passes null for a field the file leaves out.
    /// </summary>
    internal sealed record StyleEntry
    {
        public required string Name { get; init; }

        public string Description { get; init => field = value ?? string.Empty; } = string.Empty;

        public IReadOnlyList<DayOfWeek> CookingDays { get; init => field = value ?? []; } = [];

        /// <summary>Null when the file leaves it out, which means yes.</summary>
        public bool? LeftoverLunches { get; init; }

        public int Breakfasts { get; init; }

        public int Snacks { get; init; }

        public int Sweets { get; init; }

        public int Drinks { get; init; }

        public IReadOnlyList<RuleEntry> Rules { get; init => field = value ?? []; } = [];
    }

    /// <summary>One rule of a starter style. See <see cref="Managers.Contracts.Messages.PlanRuleDetails"/>.</summary>
    internal sealed record RuleEntry
    {
        public RuleLimit Limit { get; init; }

        public int Count { get; init; }

        public MealSlot? Slot { get; init; }

        public RecipeMatchKind Match { get; init; }

        public IReadOnlyList<string> Values { get; init => field = value ?? []; } = [];

        public int? Minutes { get; init; }

        public DayOfWeek? Day { get; init; }
    }

    /// <summary>The plan style file's JSON shape, made at build time. Days are written by name.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(StyleFile))]
    private sealed partial class StyleJson : JsonSerializerContext;
}
