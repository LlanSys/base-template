using Microsoft.EntityFrameworkCore;
using BT.Domain.Shared.Contracts.Common;

namespace BT.Persistence.Features.Shared.DataContext;

/// <summary>
/// Derived DbContext used exclusively for generating and routing SQL Server migrations.
/// </summary>
public class SharedSqlServerDBContext : SharedDBContext
{
    public SharedSqlServerDBContext(
        DbContextOptions<SharedSqlServerDBContext> options,
        ICurrentTenantProvider tenantProvider,
        ICurrentActorProvider actorProvider,
        Microsoft.Extensions.Logging.ILogger<SharedDBContext>? logger = null,
        TimeProvider? timeProvider = null)
        : base(options, tenantProvider, actorProvider, logger, timeProvider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SharedSqlServerDBContext).Assembly,
            type => type.Namespace?.StartsWith("BT.Persistence.Features.Shared.EntityConfigurations.SqlServer", StringComparison.Ordinal) == true);
    }
}

