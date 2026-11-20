using ChefBot.Core.Engines.Transformation;

namespace ChefBot.Core.Tests;

public class DurationParserTests
{
    [Test]
    [Arguments("PT1H30M", 90)]
    [Arguments("pt45m", 45)]
    [Arguments("P0DT2H", 120)]
    [Arguments("30", 30)]
    [Arguments("45 min", 45)]
    [Arguments("1 hour 15 minutes", 75)]
    [Arguments("1h 30m", 90)]
    [Arguments("1.5 hours", 90)]
    [Arguments("About 20 mins", 20)]
    public async Task Reads_times_as_minutes(string text, int minutes)
    {
        await Assert.That(DurationParser.Minutes(text)).IsEqualTo(minutes);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("a while")]
    [Arguments("PTnonsense")]
    [Arguments("0")]
    public async Task Gives_nothing_for_text_without_a_time(string? text)
    {
        await Assert.That(DurationParser.Minutes(text)).IsNull();
    }
}
