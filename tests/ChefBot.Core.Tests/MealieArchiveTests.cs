using System.IO.Compression;
using System.Text;
using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class MealieArchiveTests
{
    private const string ChiliId = "5cf56e46-1cc0-4a0f-95ba-ab465f68f775";
    private const string BundtId = "cbb1a756-64d5-4ad4-87b0-c062a10f1a9d";
    private const string SoupId = "a4a9b9ca-2e7d-4690-99a3-9bd1d8954149";

    private static readonly TransformationEngine Reader = TestData.Transformation;

    private static readonly byte[] Webp = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, 1, 2, 3];

    private static readonly byte[] SmallWebp = [.. "RIFF"u8, 0, 0, 0, 0, .. "WEBP"u8, 9];

    // The bundt's id is written without dashes, as older Mealie versions do; its folder still has them.
    private static readonly string Database = $$"""
        {
          "recipes": [
            { "id": "{{ChiliId}}", "name": "Chili", "recipe_servings": 6.0 },
            { "id": "{{BundtId.Replace("-", string.Empty, StringComparison.Ordinal)}}", "name": "Lemon Bundt", "recipe_servings": 12.0 },
            { "id": "{{SoupId}}", "name": "Soup", "recipe_servings": 4.0 }
          ],
          "recipes_ingredients": [
            { "recipe_id": "{{ChiliId}}", "position": 0, "note": "1 lb ground beef" },
            { "recipe_id": "{{BundtId.Replace("-", string.Empty, StringComparison.Ordinal)}}", "position": 0, "note": "2 lemons" },
            { "recipe_id": "{{SoupId}}", "position": 0, "note": "1 onion" }
          ]
        }
        """;

    /// <summary>A Mealie backup .zip holding <paramref name="entries"/>, by path.</summary>
    private static MemoryStream Zip(params (string Path, byte[] Data)[] entries)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, data) in entries)
            {
                using var entry = zip.CreateEntry(path).Open();
                entry.Write(data);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream Backup() => Zip(
        ("database.json", Encoding.UTF8.GetBytes(Database)),
        ($"data/recipes/{ChiliId}/images/min-original.webp", SmallWebp),
        ($"data/recipes/{ChiliId}/images/original.webp", Webp),
        ($"data/recipes/{ChiliId}/images/tiny-original.webp", SmallWebp),
        ($"data/recipes/{BundtId}/images/original.webp", Webp),
        ($"data/recipes/{SoupId}/images/original.webp", new byte[ValidationEngine.MaxImageBytes + 1]),
        ("data/users/fb06536d-223f-4502-b1cb-0ea209c81b4a/profile.webp", SmallWebp));

    [Test]
    public async Task Reads_recipes_and_their_photos_from_a_backup_zip()
    {
        var file = Reader.ReadArchive("mealie_2026.11.20.zip", Backup());

        await Assert.That(file.Recipes.Select(recipe => recipe.Name)).IsEquivalentTo(["Chili", "Lemon Bundt", "Soup"]);
        await Assert.That(file.Photos!.Keys).IsEquivalentTo(["Chili", "Lemon Bundt"]);
        await Assert.That(file.Photos["chili"]).IsEquivalentTo(Webp);
        await Assert.That(file.Photos["Lemon Bundt"]).IsEquivalentTo(Webp);
    }

    [Test]
    public async Task Explains_archives_it_cannot_read()
    {
        var noDatabase = Assert.Throws<RuleViolationException>(() => Reader.ReadArchive("backup.zip", Zip(("readme.txt", "hi"u8.ToArray()))));
        var notZip = Assert.Throws<RuleViolationException>(() => Reader.ReadArchive("backup.zip", new MemoryStream("not a zip"u8.ToArray())));
        var otherKind = Assert.Throws<RuleViolationException>(() => Reader.ReadArchive("backup.tar", Backup()));
        var asText = Assert.Throws<RuleViolationException>(() => Reader.ReadFile("backup.zip", "PK"));

        await Assert.That(noDatabase.Message).Contains("no database.json");
        await Assert.That(notZip.Message).Contains("isn't a .zip file");
        await Assert.That(otherKind.Message).Contains("can't import .tar files");
        await Assert.That(asText.Message).Contains("uploaded as it is");
    }

    [Test]
    public async Task Importing_a_backup_zip_stores_each_recipes_photo()
    {
        var core = new TestCore();

        var result = await core.RecipeManager().ImportArchiveAsync("backup.zip", Backup(), CancellationToken.None);

        await Assert.That(result).IsEqualTo(new FileImportResult(3, 0, 0, 2));
        var chili = core.Recipes.Rows.Single(recipe => recipe.Name == "Chili");
        var photo = chili.Images.Single();
        await Assert.That(photo.ContentType).IsEqualTo("image/webp");
        await Assert.That(photo.SourceUrl).IsNull();
        await Assert.That(core.Recipes.ImageFiles[photo.Id].Data).IsEquivalentTo(Webp);
        await Assert.That(core.Recipes.Rows.Single(recipe => recipe.Name == "Soup").Images).IsEmpty();
        await Assert.That(core.Messages.Published.OfType<FetchRecipeImages>()).IsEmpty();
    }

    [Test]
    public async Task Importing_a_backup_zip_again_adds_photos_only_to_recipes_without_any()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();
        await recipes.ImportFileAsync(new ImportFileRequest("database.json", Database), CancellationToken.None);
        var bundt = core.Recipes.Rows.Single(recipe => recipe.Name == "Lemon Bundt");
        var own = (await recipes.AddImageAsync(bundt.Id, new AddRecipeImageRequest(SmallWebp), CancellationToken.None)).Images.Single();

        var first = await recipes.ImportArchiveAsync("backup.zip", Backup(), CancellationToken.None);
        var again = await recipes.ImportArchiveAsync("backup.zip", Backup(), CancellationToken.None);

        await Assert.That(first).IsEqualTo(new FileImportResult(0, 3, 0, 1));
        await Assert.That(again).IsEqualTo(new FileImportResult(0, 3, 0, 0));
        await Assert.That(core.Recipes.Rows.Single(recipe => recipe.Name == "Chili").Images.Count).IsEqualTo(1);
        await Assert.That(core.Recipes.Rows.Single(recipe => recipe.Name == "Lemon Bundt").Images.Single().Id).IsEqualTo(own.Id);
    }
}
