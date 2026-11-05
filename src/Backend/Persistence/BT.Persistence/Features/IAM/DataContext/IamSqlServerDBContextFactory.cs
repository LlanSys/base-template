using BT.Persistence.Common.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BT.Persistence.Features.IAM.DataContext;

public class IamSqlServerDBContextFactory : IDesignTimeDbContextFactory<IamSqlServerDBContext>
{
    public IamSqlServerDBContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfigurationFactory.Create();
        var optionsBuilder = new DbContextOptionsBuilder<IamSqlServerDBContext>();
        var connectionString = DesignTimeConfigurationFactory.GetConnectionString(configuration, "IamConnection", fallbackConnectionName: "DefaultSqlConnection");

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions => DesignTimeConfigurationFactory.ConfigureSqlServer(sqlOptions, "__EFMigrationsHistory_IAM"))
            .ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, Shared.Migrations.Generators.IdempotentSqlServerMigrationsSqlGenerator>();

        return new IamSqlServerDBContext(optionsBuilder.Options);
    }
}
