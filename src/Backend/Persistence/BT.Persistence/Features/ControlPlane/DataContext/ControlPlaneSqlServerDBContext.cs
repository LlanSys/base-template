using BT.Domain.Shared.Contracts.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;

namespace BT.Persistence.Features.ControlPlane.DataContext;

public class ControlPlaneSqlServerDBContext : ControlPlaneDBContext
{
    public ControlPlaneSqlServerDBContext(
        DbContextOptions<ControlPlaneSqlServerDBContext> options,
        ICurrentActorProvider? actorProvider = null,
        ILogger<ControlPlaneSqlServerDBContext>? logger = null,
        TimeProvider? timeProvider = null)
        : base(options, actorProvider ?? new SystemActorProvider(), logger, timeProvider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ControlPlaneSqlServerDBContext).Assembly,
            type => type.Namespace?.StartsWith("BT.Persistence.Features.ControlPlane.EntityConfigurations.SqlServer", StringComparison.Ordinal) == true);
    }

    private sealed class SystemActorProvider : ICurrentActorProvider
    {
        public string ActorId => ICurrentActorProvider.SystemActor;
    }
}
