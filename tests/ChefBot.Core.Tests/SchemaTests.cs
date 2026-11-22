using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.Tests;

/// <summary>
/// Checks the database model without a database: building the model needs the provider, not a connection.
/// </summary>
public class SchemaTests
{
    /// <summary>Tables a backup leaves out on purpose, and why.</summary>
    private static readonly Dictionary<Type, string> NotBackedUp = new()
    {
        [typeof(AccessLinkEntity)] = "sign-in and invite links are short-lived; new ones are made after a restore",
        [typeof(ApiTokenEntity)] = "a token revoked since the backup must not come back",
    };

    private static ChefBotDbContext Context() =>
        new(new DbContextOptionsBuilder<ChefBotDbContext>().UseNpgsql("Host=unused").Options);

    [Test]
    public async Task Every_enum_column_is_stored_by_name()
    {
        using var db = Context();
        var byNumber = db.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum)
            .Where(property => property.GetProviderClrType() != typeof(string) || property.GetMaxLength() != 20)
            .Select(property => $"{property.DeclaringType.ClrType.Name}.{property.Name}")
            .ToList();

        await Assert.That(byNumber).IsEmpty();
    }

    [Test]
    public async Task Every_table_is_backed_up_or_left_out_on_purpose()
    {
        using var db = Context();
        var covered = BackupAccess.Tables.Select(table => table.EntityType).ToHashSet();

        // Children are written inside their parents, through the collections a table includes.
        var parents = new Queue<Type>(covered);
        while (parents.TryDequeue(out var parent))
        {
            foreach (var child in db.Model.FindEntityType(parent)!.GetNavigations().Where(n => n.IsCollection))
            {
                if (covered.Add(child.TargetEntityType.ClrType))
                {
                    parents.Enqueue(child.TargetEntityType.ClrType);
                }
            }
        }

        var missing = db.Model.GetEntityTypes()
            .Select(entity => entity.ClrType)
            .Where(type => !covered.Contains(type) && !NotBackedUp.ContainsKey(type))
            .Select(type => type.Name)
            .ToList();

        await Assert.That(missing).IsEmpty()
            .Because("a new table needs a line in BackupAccess.Tables, or an entry in NotBackedUp saying why not");
    }

    [Test]
    public async Task Backup_table_names_are_unique()
    {
        var names = BackupAccess.Tables.Select(table => table.Name).ToList();
        await Assert.That(names.Distinct().Count()).IsEqualTo(names.Count);
    }
}
