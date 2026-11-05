using System.Data.Common;
using BT.Domain.Shared.Contracts.Common;
using BT.Persistence.Common.Initialization;
using BT.Persistence.Features.Banking.DataContext;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Persistence.Features.HR.DataContext;
using BT.Persistence.Features.IAM.DataContext;
using BT.Persistence.Features.Shared.DataContext;
using BT.Persistence.Features.Shared.Migrations.Generators;
using BT.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace BT.Tests.Integration;

public sealed class FreshDatabaseInitializationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Empty_database_initialization_is_atomic_repeatable_and_preserves_existing_databases(bool postgres)
    {
        DbFixture fixture = postgres ? new PostgreSqlDbFixture() : new MsSqlDbFixture();
        await fixture.InitializeAsync();

        try
        {
            var connection = Connection(fixture, postgres, "base_fresh_certification");
            await WithContextsAsync(
                connection,
                postgres,
                async contexts =>
                {
                    Assert.True(await FreshDatabaseInitializer.InitializeAsync(contexts));

                    foreach (var context in contexts)
                    {
                        Assert.False(context.Database.HasPendingModelChanges());
                        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
                        await context.Database.MigrateAsync();
                        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
                    }

                    var control = (ControlPlaneDBContext)contexts[0];
                    var tenant = await control.Tenants.FirstAsync();
                    tenant.DisplayName = "Keep operator changes";
                    await control.SaveChangesAsync();
                    Assert.False(await FreshDatabaseInitializer.InitializeAsync(contexts));
                    Assert.Equal("Keep operator changes", (await control.Tenants.FirstAsync()).DisplayName);
                    var iam = (IamDBContext)contexts[1];
                    Assert.True(await iam.Permissions.AnyAsync(permission => permission.Key == "controlplane.view_tenants"));
                    Assert.False(await iam.Permissions.IgnoreQueryFilters().AnyAsync(permission => permission.Key == "controlplane.manage"));
                    Assert.Equal(3, await control.Modules.CountAsync());
                    Assert.True(await ((HrDBContext)contexts[3]).Employees.IgnoreQueryFilters().AnyAsync());
                    Assert.True(await ((BankingDBContext)contexts[4]).Database.CanConnectAsync());
                    Assert.Equal(0, await contexts[2].Database.SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM QRTZ_JOB_DETAILS").SingleAsync());
                });

            await WithContextsAsync(
                Connection(fixture, postgres, "base_refusal_certification"),
                postgres,
                async contexts =>
                {
                    await contexts[0].Database.EnsureCreatedAsync();
                    var count = await ((ControlPlaneDBContext)contexts[0]).Tenants.CountAsync();
                    await Assert.ThrowsAsync<InvalidOperationException>(() => FreshDatabaseInitializer.InitializeAsync(contexts));
                    Assert.Equal(count, await ((ControlPlaneDBContext)contexts[0]).Tenants.CountAsync());
                });

            await WithContextsAsync(
                Connection(fixture, postgres, "base_rollback_certification"),
                postgres,
                async contexts =>
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => FreshDatabaseInitializer.InitializeAsync(contexts));
                    Assert.False(await contexts[0].GetService<IRelationalDatabaseCreator>().HasTablesAsync());
                },
                new RejectIdentityCreation());
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static string Connection(DbFixture fixture, bool postgres, string database)
    {
        return postgres
        ? new Npgsql.NpgsqlConnectionStringBuilder(fixture.GetConnectionString()) { Database = database }.ConnectionString
        : new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(fixture.GetConnectionString()) { InitialCatalog = database }.ConnectionString;
    }

    private static async Task WithContextsAsync(string connection, bool postgres, Func<IReadOnlyList<DbContext>, Task> action, DbCommandInterceptor? fault = null)
    {
        var actor = new Scope();
        await using ControlPlaneDBContext control = postgres
            ? new ControlPlanePostgreSqlDBContext(Options<ControlPlanePostgreSqlDBContext>(connection, postgres, "ControlPlane"), actor)
            : new ControlPlaneSqlServerDBContext(Options<ControlPlaneSqlServerDBContext>(connection, postgres, "ControlPlane"), actor);

        await using IamDBContext iam = postgres
            ? new IamPostgreSqlDBContext(Options<IamPostgreSqlDBContext>(connection, postgres, "IAM", fault), actor, actor)
            : new IamSqlServerDBContext(Options<IamSqlServerDBContext>(connection, postgres, "IAM", fault), actor, actor);

        await using SharedDBContext shared = postgres
            ? new SharedPostgreSqlDBContext(Options<SharedPostgreSqlDBContext>(connection, postgres, "Shared"), actor, actor)
            : new SharedSqlServerDBContext(Options<SharedSqlServerDBContext>(connection, postgres, "Shared"), actor, actor);

        await using HrDBContext hr = postgres
            ? new HrPostgreSqlDBContext(Options<HrPostgreSqlDBContext>(connection, postgres, "HR"), actor, actor)
            : new HrSqlServerDBContext(Options<HrSqlServerDBContext>(connection, postgres, "HR"), actor, actor);

        await using BankingDBContext banking = postgres
            ? new BankingPostgreSqlDBContext(Options<BankingPostgreSqlDBContext>(connection, postgres, "Banking"), actor, actor)
            : new BankingSqlServerDBContext(Options<BankingSqlServerDBContext>(connection, postgres, "Banking"), actor, actor);

        await action([control, iam, shared, hr, banking]);
    }

    private static DbContextOptions<T> Options<T>(string connection, bool postgres, string history, DbCommandInterceptor? fault = null) where T : DbContext
    {
        var builder = new DbContextOptionsBuilder<T>();

        if (postgres)
        {
            builder.UseNpgsql(connection, pg => pg.MigrationsHistoryTable("__EFMigrationsHistory_" + history))
                .ReplaceService<IMigrationsSqlGenerator, IdempotentNpgsqlMigrationsSqlGenerator>();
        }
        else
        {
            builder.UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_" + history))
                .ReplaceService<IMigrationsSqlGenerator, IdempotentSqlServerMigrationsSqlGenerator>();
        }

        if (fault is not null)
        {
            builder.AddInterceptors(fault);
        }

        return builder.Options;
    }

    private sealed class RejectIdentityCreation : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Simulated failure after Control Plane creation.");
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed class Scope : ICurrentTenantProvider, ICurrentActorProvider
    {
        public Guid TenantId => Guid.Empty;
        public string ActorId => "00000000-0000-0000-0000-000000000001";
    }
}
