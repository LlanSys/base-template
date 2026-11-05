using BT.Domain.Shared.Contracts.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;

namespace BT.Persistence.Features.ControlPlane.DataContext;

public class ControlPlanePostgreSqlDBContext : ControlPlaneDBContext
{
    public ControlPlanePostgreSqlDBContext(
        DbContextOptions<ControlPlanePostgreSqlDBContext> options,
        ICurrentActorProvider? actorProvider = null,
        ILogger<ControlPlanePostgreSqlDBContext>? logger = null,
        TimeProvider? timeProvider = null)
        : base(options, actorProvider ?? new SystemActorProvider(), logger, timeProvider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        base.OnModelCreating(modelBuilder);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            if (entity.FindProperty("RowVersion") is { } version)
                version.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ControlPlanePostgreSqlDBContext).Assembly,
            type => type.Namespace?.StartsWith("BT.Persistence.Features.ControlPlane.EntityConfigurations.PostgreSql", StringComparison.Ordinal) == true);
    }

    private sealed class SystemActorProvider : ICurrentActorProvider
    {
        public string ActorId => ICurrentActorProvider.SystemActor;
    }
}
