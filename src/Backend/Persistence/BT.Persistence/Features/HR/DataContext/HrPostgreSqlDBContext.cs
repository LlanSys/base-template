using BT.Domain.Shared.Contracts.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BT.Persistence.Features.HR.DataContext;

/// <summary>
/// Derived DbContext used exclusively for generating and routing PostgreSQL migrations.
/// </summary>
public class HrPostgreSqlDBContext : HrDBContext
{
    public HrPostgreSqlDBContext(
        DbContextOptions<HrPostgreSqlDBContext> options,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<HrDBContext>? logger = null,
        TimeProvider? timeProvider = null)
        : base(options, tenantProvider, actorProvider, logger, timeProvider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(HrPostgreSqlDBContext).Assembly,
            type => type.Namespace?.StartsWith("BT.Persistence.Features.HR.EntityConfigurations.PostgreSql", StringComparison.Ordinal) == true);
    }
}
