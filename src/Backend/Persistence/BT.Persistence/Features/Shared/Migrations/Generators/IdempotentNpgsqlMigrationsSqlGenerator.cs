using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace BT.Persistence.Features.Shared.Migrations.Generators;

#pragma warning disable EF1001 // Internal EF Core API usage.
public class IdempotentNpgsqlMigrationsSqlGenerator : NpgsqlMigrationsSqlGenerator
{
    public IdempotentNpgsqlMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        INpgsqlSingletonOptions npgsqlSingletonOptions)
        : base(dependencies, npgsqlSingletonOptions)
    {
    }

    protected override void Generate(CreateIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        var dummyBuilder = new MigrationCommandListBuilder(Dependencies);
        base.Generate(operation, model, dummyBuilder, terminate: false);
        dummyBuilder.EndCommand();

        var commands = dummyBuilder.GetCommandList();
        if (commands is [{ } command, ..])
        {
            var sql = command.CommandText;
            sql = Regex.Replace(sql, @"^CREATE\s+(UNIQUE\s+)?INDEX(?!\s+IF\s+NOT\s+EXISTS)", "CREATE $1INDEX IF NOT EXISTS", RegexOptions.IgnoreCase);

            builder.Append(sql);
        }

        if (terminate)
        {
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            builder.EndCommand();
        }
    }
    protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        // NpgsqlHistoryRepository adds IF NOT EXISTS to the generated history-table SQL.
        // Leave that model to the provider so its second pass does not duplicate the clause.
        if (model?.FindEntityType(typeof(HistoryRow)) is not null)
        {
            base.Generate(operation, model, builder, terminate);
            return;
        }

        var dummyBuilder = new MigrationCommandListBuilder(Dependencies);
        base.Generate(operation, model, dummyBuilder, terminate: false);
        dummyBuilder.EndCommand();

        var commands = dummyBuilder.GetCommandList();
        if (commands is [{ } command, ..])
        {
            var sql = command.CommandText;
            sql = Regex.Replace(sql, @"^CREATE\s+TABLE(?!\s+IF\s+NOT\s+EXISTS)", "CREATE TABLE IF NOT EXISTS", RegexOptions.IgnoreCase);

            builder.Append(sql);
        }

        if (terminate)
        {
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            builder.EndCommand();
        }
    }
}
#pragma warning restore EF1001 // Internal EF Core API usage.
