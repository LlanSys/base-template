using BT.Persistence.Common.Initialization;
using BT.Persistence.Features.Banking.DataContext;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Persistence.Features.HR.DataContext;
using BT.Persistence.Features.IAM.DataContext;
using BT.Persistence.Features.Shared.DataContext;
using BT.Persistence.Features.Shared.Migrations.Generators;
using BT.Domain.Shared.Contracts.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace BT.DatabaseTools;

internal static class Program
{
    private enum Provider { SqlServer, PostgreSql }
    private static async Task<int> Main(string[] args)
    {
        if (args.Length != 2 || !Enum.TryParse<Provider>(args[0], true, out var provider) || !Enum.IsDefined(provider)
            || args[1] is not ("fresh" or "upgrade" or "status" or "reprotect-local-authenticator"))
        {
            Console.Error.WriteLine("Usage: BT.DatabaseTools SqlServer|PostgreSql fresh|upgrade|status|reprotect-local-authenticator. Set BT_DATABASE_CONNECTION through the environment; never pass credentials as arguments.");
            return 2;
        }
        var connection = Environment.GetEnvironmentVariable("BT_DATABASE_CONNECTION");

        if (string.IsNullOrWhiteSpace(connection)) { Console.Error.WriteLine("BT_DATABASE_CONNECTION must explicitly identify the target application database."); return 2; }
        var actor = new InitializationScope();

        try
        {
            ControlPlaneDBContext control = provider == Provider.SqlServer
                ? new ControlPlaneSqlServerDBContext(Options<ControlPlaneSqlServerDBContext>(provider, connection, "ControlPlane"), actor)
                : new ControlPlanePostgreSqlDBContext(Options<ControlPlanePostgreSqlDBContext>(provider, connection, "ControlPlane"), actor);

            await using var controlLifetime = control.ConfigureAwait(false);
            IamDBContext iam = provider == Provider.SqlServer
                ? new IamSqlServerDBContext(Options<IamSqlServerDBContext>(provider, connection, "IAM"), actor, actor)
                : new IamPostgreSqlDBContext(Options<IamPostgreSqlDBContext>(provider, connection, "IAM"), actor, actor);

            await using var iamLifetime = iam.ConfigureAwait(false);
            SharedDBContext shared = provider == Provider.SqlServer
                ? new SharedSqlServerDBContext(Options<SharedSqlServerDBContext>(provider, connection, "Shared"), actor, actor)
                : new SharedPostgreSqlDBContext(Options<SharedPostgreSqlDBContext>(provider, connection, "Shared"), actor, actor);

            await using var sharedLifetime = shared.ConfigureAwait(false);
            HrDBContext hr = provider == Provider.SqlServer
                ? new HrSqlServerDBContext(Options<HrSqlServerDBContext>(provider, connection, "HR"), actor, actor)
                : new HrPostgreSqlDBContext(Options<HrPostgreSqlDBContext>(provider, connection, "HR"), actor, actor);

            await using var hrLifetime = hr.ConfigureAwait(false);
            BankingDBContext banking = provider == Provider.SqlServer
                ? new BankingSqlServerDBContext(Options<BankingSqlServerDBContext>(provider, connection, "Banking"), actor, actor)
                : new BankingPostgreSqlDBContext(Options<BankingPostgreSqlDBContext>(provider, connection, "Banking"), actor, actor);

            await using var bankingLifetime = banking.ConfigureAwait(false);
            DbContext[] contexts = [control, iam, shared, hr, banking];

            if (args[1] == "fresh")
                Console.WriteLine(await FreshDatabaseInitializer.InitializeAsync(contexts).ConfigureAwait(false) ? "Fresh database initialized atomically; migration histories match the current models." : "Database is already initialized; nothing changed.");
            else if (args[1] == "upgrade")
            {
                var database = control.Database.GetDbConnection().Database;
                string[] reserved = ["master", "model", "msdb", "tempdb", "postgres", "template0", "template1"];

                if (string.IsNullOrWhiteSpace(database) || reserved.Contains(database, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Choose an explicit application database.");
                var creator = control.GetService<IRelationalDatabaseCreator>();

                if (!await creator.ExistsAsync().ConfigureAwait(false) || !await creator.HasTablesAsync().ConfigureAwait(false))
                    throw new InvalidOperationException("Use fresh initialization for an empty database.");

                foreach (var context in contexts) await context.Database.MigrateAsync().ConfigureAwait(false);
                Console.WriteLine("Existing database migrations applied.");
            }
            else if (args[1] == "reprotect-local-authenticator")
                await LocalAuthenticatorReprotection.RunAsync(iam, TimeProvider.System).ConfigureAwait(false);
            else
            {
                foreach (var context in contexts)
                    Console.WriteLine($"{context.GetType().Name}: {(await context.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).Count()} pending migrations.");
            }
            return 0;
        }
        catch (Exception exception)
        {
            // Connection/provider exception text can include secrets. Keep CLI output safe.
            Console.Error.WriteLine("Database operation failed. No database is reset or dropped. Check target/provider configuration, model consistency, and whether fresh initialization was requested for a non-empty database.");
            Console.Error.WriteLine(exception switch
            {
                Microsoft.Data.SqlClient.SqlException sql => $"SQL Server diagnostic code: {sql.Number}.",
                Npgsql.PostgresException pg => $"PostgreSQL diagnostic code: {pg.SqlState}.",
                InvalidOperationException => "Initialization preconditions or model consistency checks failed.",
                System.Security.Cryptography.CryptographicException => "The original or active key store could not protect the authenticator. Its enrollment was not reset or disabled.",
                _ => "Check connectivity and the selected provider."
            });

            return 1;
        }
    }
    private static DbContextOptions<T> Options<T>(Provider provider, string connection, string history) where T : DbContext
    {
        var options = new DbContextOptionsBuilder<T>();

        switch (provider)
        {
            case Provider.SqlServer:
                options.UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_" + history))
                    .ReplaceService<IMigrationsSqlGenerator, IdempotentSqlServerMigrationsSqlGenerator>(); break;
            case Provider.PostgreSql:
                options.UseNpgsql(connection, pg => pg.MigrationsHistoryTable("__EFMigrationsHistory_" + history))
                    .ReplaceService<IMigrationsSqlGenerator, IdempotentNpgsqlMigrationsSqlGenerator>(); break;
            default: throw new InvalidOperationException("Unsupported provider.");
        }
        return options.Options;
    }
    private sealed class InitializationScope : ICurrentTenantProvider, ICurrentActorProvider
    {
        public Guid TenantId => Guid.Empty;
        public string ActorId => "00000000-0000-0000-0000-000000000001";
    }
}
