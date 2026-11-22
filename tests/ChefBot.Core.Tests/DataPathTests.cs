using Microsoft.Extensions.Configuration;

namespace ChefBot.Core.Tests;

/// <summary>
/// Where ChefBot looks for its files: what it ships next to the app, and what it writes under Data:Root, so the
/// container image, the Windows service and the portable app each keep their data in one folder.
/// </summary>
public class DataPathTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "chefbot-data");

    private static IConfiguration Settings(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();

    [Test]
    public async Task Without_a_data_root_what_ChefBot_writes_goes_in_the_app_folder()
    {
        var backups = CoreRegistration.StoragePath(Settings(), "Data:Backups", "backups");

        await Assert.That(backups).IsEqualTo(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "backups")));
    }

    [Test]
    public async Task What_ChefBot_writes_goes_under_the_data_root()
    {
        var settings = Settings(("Data:Root", Root));

        await Assert.That(CoreRegistration.StoragePath(settings, "Data:Backups", "backups")).IsEqualTo(Path.Combine(Root, "backups"));
        await Assert.That(CoreRegistration.StoragePath(settings, "Data:SessionKeys", "keys")).IsEqualTo(Path.Combine(Root, "keys"));
    }

    [Test]
    public async Task A_relative_setting_is_relative_to_the_data_root_and_a_full_path_is_used_as_it_is()
    {
        var elsewhere = Path.Combine(Path.GetTempPath(), "elsewhere");
        var settings = Settings(("Data:Root", Root), ("Data:Backups", "nightly"), ("Data:SessionKeys", elsewhere));

        await Assert.That(CoreRegistration.StoragePath(settings, "Data:Backups", "backups")).IsEqualTo(Path.Combine(Root, "nightly"));
        await Assert.That(CoreRegistration.StoragePath(settings, "Data:SessionKeys", "keys")).IsEqualTo(elsewhere);
    }

    [Test]
    public async Task Shipped_files_stay_in_the_app_folder_whatever_the_data_root()
    {
        var catalog = CoreRegistration.DataPath(Settings(("Data:Root", Root)), "Data:IngredientCatalog", "config/ingredients.json");

        await Assert.That(catalog).IsEqualTo(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "config/ingredients.json")));
        await Assert.That(File.Exists(catalog)).IsTrue();
    }

    [Test]
    public async Task A_relative_data_root_is_relative_to_the_app_folder()
    {
        var root = CoreRegistration.DataRoot(Settings(("Data:Root", "ChefBot-data")));

        await Assert.That(root).IsEqualTo(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "ChefBot-data")));
    }
}
