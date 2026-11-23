using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.Tests;

/// <summary>Checks the migrations against the model, without a database.</summary>
public class MigrationTests
{
    private static ChefBotDbContext Context() =>
        new(new DbContextOptionsBuilder<ChefBotDbContext>().UseNpgsql("Host=unused").Options);

    [Test]
    public async Task Every_model_change_has_a_migration()
    {
        using var db = Context();
        await Assert.That(db.Database.HasPendingModelChanges()).IsFalse()
            .Because("an entity changed without a migration: run `dotnet ef migrations add <Name> --project src/ChefBot.Core "
                + "--output-dir ResourceAccess/Persistence/Migrations` (AGENTS.md)");
    }

    [Test]
    public async Task The_baseline_is_the_first_migration()
    {
        using var db = Context();
        await Assert.That(db.Database.GetMigrations().First()).IsEqualTo(DatabaseUpgrade.BaselineMigration);
    }

    [Test]
    [Arguments(true, false, true)]
    [Arguments(true, true, false)]
    [Arguments(false, false, false)]
    [Arguments(false, true, false)]
    public async Task Only_a_database_made_before_migrations_is_adopted_at_the_baseline(bool hasTables, bool hasApplied, bool adopted)
    {
        await Assert.That(DatabaseUpgrade.NeedsAdopting(hasTables, hasApplied)).IsEqualTo(adopted);
    }
}
