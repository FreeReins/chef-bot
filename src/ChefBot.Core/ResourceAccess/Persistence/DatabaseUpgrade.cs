using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <summary>
/// Brings the database up to date with the migrations in <c>Persistence/Migrations</c>. A fresh database gets every
/// migration. A database made before ChefBot used migrations (by <c>EnsureCreated</c>) already has the
/// <see cref="BaselineMigration"/> tables, so it is first marked as being at the baseline and then gets only what came after.
/// </summary>
internal static class DatabaseUpgrade
{
    /// <summary>The schema as it was when ChefBot switched to migrations. Never edit or rename it.</summary>
    public const string BaselineMigration = "20261123025222_Baseline";

    /// <summary>A table every ChefBot database has had from the start, in the default schema.</summary>
    private const string KnownTable = "Users";

    public static async Task RunAsync(ChefBotDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var database = context.Database;
        var history = database.GetService<IHistoryRepository>();
        var applied = await database.GetAppliedMigrationsAsync(cancellationToken);
        if (NeedsAdopting(await TableExistsAsync(database, KnownTable, cancellationToken), applied.Any()))
        {
            await using var transaction = await database.BeginTransactionAsync(cancellationToken);
            await database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), cancellationToken);
            await database.ExecuteSqlRawAsync(
                history.GetInsertScript(new HistoryRow(BaselineMigration, ProductInfo.GetVersion())), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("The database was made before ChefBot used migrations. It is now marked as being at {Baseline}.", BaselineMigration);
        }

        await database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Whether the database has ChefBot's tables but no migration recorded as applied: it was made by
    /// <c>EnsureCreated</c>. The history table may already exist and be empty, since taking the migration lock creates it.
    /// </summary>
    public static bool NeedsAdopting(bool hasChefBotTables, bool hasAppliedMigrations) => hasChefBotTables && !hasAppliedMigrations;

    private static Task<bool> TableExistsAsync(DatabaseFacade database, string table, CancellationToken cancellationToken) =>
        database.SqlQuery<bool>($"""
            SELECT EXISTS (SELECT 1 FROM information_schema.tables
                           WHERE table_schema = current_schema() AND table_name = {table}) AS "Value"
            """).SingleAsync(cancellationToken);
}
