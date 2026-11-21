using ChefBot.Core.Utilities;

namespace ChefBot.Core.Tests;

public class UnitTests
{
    [Test]
    public async Task Converts_within_volume_and_weight()
    {
        await Assert.That(Units.TryConvert(1m, "cup", "tbsp", out var tablespoons)).IsTrue();
        await Assert.That(tablespoons).IsEqualTo(16m);
        await Assert.That(Units.TryConvert(16m, "ounces", "lb", out var pounds)).IsTrue();
        await Assert.That(Math.Round(pounds, 3)).IsEqualTo(1m);
    }

    [Test]
    public async Task Does_not_mix_volume_weight_or_counts()
    {
        await Assert.That(Units.TryConvert(1m, "cup", "lb", out _)).IsFalse();
        await Assert.That(Units.TryConvert(1m, "can", "cup", out _)).IsFalse();
        await Assert.That(Units.Comparable("tbsp", "ml")).IsTrue();
        await Assert.That(Units.Comparable("clove", "")).IsFalse();
        await Assert.That(Units.Family("cloves")).IsEqualTo("clove");
    }

    [Test]
    [Arguments(24, "tbsp", 1.5, "cup")]
    [Arguments(3, "tsp", 1, "tbsp")]
    [Arguments(0.5, "tsp", 0.5, "tsp")]
    [Arguments(20, "oz", 1.25, "lb")]
    [Arguments(1500, "g", 1.5, "kg")]
    [Arguments(0.25, "l", 250, "ml")]
    [Arguments(2, "can", 2, "can")]
    public async Task Shows_totals_in_a_readable_unit(double quantity, string unit, double expected, string expectedUnit)
    {
        var (amount, shown) = Units.Readable((decimal)quantity, unit);

        await Assert.That(amount).IsEqualTo((decimal)expected);
        await Assert.That(shown).IsEqualTo(expectedUnit);
    }
}
