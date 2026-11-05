using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using BT.Persistence.Features.Shared.DataContext;

namespace BT.Persistence.Common.Initialization;

/// <summary>Creates an empty database from the current migration-consistent model without rewriting historical upgrades.</summary>
public static class FreshDatabaseInitializer
{
    public static async Task<bool> InitializeAsync(IReadOnlyList<DbContext> contexts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        if (contexts.Count == 0) throw new ArgumentException("At least one context is required.", nameof(contexts));
        var first = contexts[0];
        var connection = first.Database.GetDbConnection();
        var reserved = new[] { "master", "model", "msdb", "tempdb", "postgres", "template0", "template1" };

        if (string.IsNullOrWhiteSpace(connection.Database) || reserved.Contains(connection.Database, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose an explicit application database, not a system database.");

        foreach (var context in contexts)
        {
            if (context.Database.ProviderName != first.Database.ProviderName || context.Database.GetDbConnection().ConnectionString != connection.ConnectionString)
                throw new InvalidOperationException("All contexts must use the same provider and application database.");

            if (context.Database.HasPendingModelChanges() || !context.Database.GetMigrations().Any())
                throw new InvalidOperationException("Generate and verify migrations for every context before initializing a database.");
        }
        var creator = first.GetService<IRelationalDatabaseCreator>();

        if (!await creator.ExistsAsync(cancellationToken).ConfigureAwait(false))
            await creator.CreateAsync(cancellationToken).ConfigureAwait(false);

        if (await creator.HasTablesAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var context in contexts)
                if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
                    throw new InvalidOperationException("This database already contains tables. Use the existing-database upgrade path; fresh initialization changes nothing.");
            return false;
        }
        // The exact provider connections are shared so schema, seeds, and every context's history commit together.
        foreach (var context in contexts.Skip(1)) context.Database.SetDbConnection(connection, contextOwnsConnection: false);
        await first.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await first.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var transaction = await first.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var transactionLifetime = transaction.ConfigureAwait(false);

            try
            {
                foreach (var context in contexts.Skip(1))
                    await context.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
                // Recheck under the shared transaction. Concurrent initializers must never be mistaken for an empty target.
                if (await creator.HasTablesAsync(cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException("The database changed during initialization. Retry after checking its state.");

                foreach (var context in contexts)
                {
                    await context.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(cancellationToken).ConfigureAwait(false);

                    if (context is SharedDBContext)
                        await CreateSchedulerSchemaAsync(context, cancellationToken).ConfigureAwait(false);
                    var history = context.GetService<IHistoryRepository>();
                    await context.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), cancellationToken).ConfigureAwait(false);

                    foreach (var migration in context.Database.GetMigrations())
                        await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(migration, ProductInfo.GetVersion())), cancellationToken).ConfigureAwait(false);
                }
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // Detach borrowed transactions even after cancellation so a retry cannot reuse a completed transaction.
                foreach (var context in contexts.Skip(1))
                    await context.Database.UseTransactionAsync(null, CancellationToken.None).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        return true;
    }

    private static async Task CreateSchedulerSchemaAsync(DbContext context, CancellationToken cancellationToken)
    {
        // Quartz owns tables outside the EF model. Retain its provider-specific schema without
        // replaying the historical reset preamble against an already initialized database.
        var assembly = context.GetService<IMigrationsAssembly>();
        var migration = assembly.Migrations.Single(entry => entry.Key.EndsWith("_QuartzInitialSchema", StringComparison.Ordinal));
        var schema = assembly.CreateMigration(migration.Value, context.Database.ProviderName!).UpOperations.OfType<SqlOperation>().Single().Sql;
        var start = schema.IndexOf("CREATE TABLE", StringComparison.OrdinalIgnoreCase);

        if (start < 0) throw new InvalidOperationException("The scheduler schema baseline is unavailable.");
        var commands = context.GetService<IMigrationsSqlGenerator>().Generate([new SqlOperation { Sql = schema[start..] }], context.Model);

        foreach (var command in commands)
            await context.Database.ExecuteSqlRawAsync(command.CommandText, cancellationToken).ConfigureAwait(false);
    }
}
