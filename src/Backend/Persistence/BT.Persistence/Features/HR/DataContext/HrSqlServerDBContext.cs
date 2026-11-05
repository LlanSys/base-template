using BT.Domain.Shared.Contracts.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BT.Persistence.Features.HR.DataContext;

/// <summary>
/// Derived DbContext used exclusively for generating and routing SQL Server migrations.
/// </summary>
public class HrSqlServerDBContext : HrDBContext
{
    public HrSqlServerDBContext(
        DbContextOptions<HrSqlServerDBContext> options,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<HrDBContext>? logger = null,
        TimeProvider? timeProvider = null)
        : base(options, tenantProvider, actorProvider, logger, timeProvider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(HrSqlServerDBContext).Assembly,
            type => type.Namespace?.StartsWith("BT.Persistence.Features.HR.EntityConfigurations.SqlServer", StringComparison.Ordinal) == true);
    }
}
