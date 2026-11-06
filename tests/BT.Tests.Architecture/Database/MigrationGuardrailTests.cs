using BT.Domain.Shared.Contracts.Common;
using BT.Persistence.Features.Shared.DataContext;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace BT.Tests.Architecture.Database;

[Trait("Category", "Guardrail")]
public class MigrationGuardrailTests
{
    private const string ContextSuffix = "DBContext";
    private const string SqlServer = "SqlServer";
    private const string PostgreSql = "PostgreSql";

    public static TheoryData<Type, string> ProviderContexts()
    {
        var cases = new TheoryData<Type, string>();
        var contextTypes = typeof(SharedDBContext).Assembly.GetTypes()
            .Where(type => !type.ContainsGenericParameters
                && type.IsSubclassOf(typeof(DbContext))
                && !type.Name.EndsWith(SqlServer + ContextSuffix, StringComparison.Ordinal)
                && !type.Name.EndsWith(PostgreSql + ContextSuffix, StringComparison.Ordinal))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (var contextType in contextTypes)
        {
            cases.Add(contextType, SqlServer);
            cases.Add(contextType, PostgreSql);
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(ProviderContexts))]
    public void ProviderContext_ShouldHaveCurrentMigrations(Type baseContextType, string providerName)
    {
        ArgumentNullException.ThrowIfNull(baseContextType);
        Assert.EndsWith(ContextSuffix, baseContextType.Name, StringComparison.Ordinal);
        var expectedName = baseContextType.Name[..^ContextSuffix.Length] + providerName + ContextSuffix;
        var contextType = baseContextType.Assembly.GetType(baseContextType.Namespace + "." + expectedName);

        Assert.True(contextType is { IsAbstract: false } && contextType.IsSubclassOf(baseContextType),
            $"Add the concrete {expectedName} for {baseContextType.Name}.");

        var builderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
        var options = (DbContextOptionsBuilder)Activator.CreateInstance(builderType)!;
        if (providerName == SqlServer)
        {
            options.UseSqlServer("Server=.;Database=dummy;Integrated Security=true");
        }
        else
        {
            options.UseNpgsql("Host=localhost;Database=dummy;Username=postgres;Password=postgres");
        }

        var services = new ServiceCollection();
        services.AddScoped<ICurrentTenantProvider, DummyTenantProvider>();
        services.AddScoped<ICurrentActorProvider, DummyActorProvider>();
        services.AddLogging();

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        using var context = (DbContext)ActivatorUtilities.CreateInstance(scope.ServiceProvider, contextType, options.Options);
        Assert.True(context.Database.GetMigrations().Any() && context.GetService<IMigrationsAssembly>().ModelSnapshot is not null,
            $"Create an initial migration and snapshot for {expectedName}.");
        Assert.False(context.Database.HasPendingModelChanges(),
            $"The model for {expectedName} differs from its snapshot. Run 'dotnet ef migrations add' for this context.");
    }

    private sealed class DummyTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId => Guid.Empty;
    }

    private sealed class DummyActorProvider : ICurrentActorProvider
    {
        public string ActorId => "System";
    }
}
