using BT.Domain.Features.ControlPlane.Auditing.Entities;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Domain.Features.ControlPlane.Modules.Entities;
using BT.Domain.Shared.Entities;
using BT.Domain.Shared.Contracts.Common;
using BT.Persistence.Common;
using BT.Persistence.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BT.Persistence.Features.ControlPlane.DataContext;

public class ControlPlaneDBContext : DbContext
{
    private readonly ICurrentActorProvider? _actorProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ControlPlaneDBContext>? _logger;

    public ControlPlaneDBContext(
        DbContextOptions<ControlPlaneDBContext> options,
        ICurrentActorProvider actorProvider,
        ILogger<ControlPlaneDBContext>? logger = null,
        TimeProvider? timeProvider = null
    ) : base(options)
    {
        _actorProvider = actorProvider;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected ControlPlaneDBContext(
        DbContextOptions options,
        ICurrentActorProvider actorProvider,
        ILogger<ControlPlaneDBContext>? logger = null,
        TimeProvider? timeProvider = null
    ) : base(options)
    {
        _actorProvider = actorProvider;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public DbSet<TenantAdministratorInvitation> AdministratorInvitations { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<ModuleDefinition> Modules { get; set; }
    public DbSet<TenantModule> TenantModules { get; set; }
    public DbSet<DeploymentStamp> DeploymentStamps { get; set; }
    public DbSet<ImpersonationRecord> ImpersonationRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ControlPlaneDBContext).Assembly,
            type => type.Namespace?.StartsWith("BT.Persistence.Features.ControlPlane", StringComparison.Ordinal) == true &&
                    !(type.Namespace?.Contains("SqlServer") == true) &&
                    !(type.Namespace?.Contains("PostgreSql") == true));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (Database.IsNpgsql())
                foreach (var entry in ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified).ToArray())
                    if (entry.Metadata.FindProperty("RowVersion") is not null)
                        entry.Property("RowVersion").CurrentValue = Guid.CreateVersion7().ToByteArray();
            // Note: Control Plane is not tenant-filtered. TenantId is Guid.Empty.
            var now = _timeProvider.GetUtcNow();
            DBContextHelper.UpdateAuditAndSoftDelete(ChangeTracker, _actorProvider?.ActorId ?? ICurrentActorProvider.SystemActor, Guid.Empty, now);

            foreach (var entry in ChangeTracker.Entries<Tenant>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
                entry.Entity.Revision = Guid.CreateVersion7();

            foreach (var entry in ChangeTracker.Entries<DeploymentStamp>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
                entry.Entity.Revision = Guid.CreateVersion7();
            Set<AdministrationAuditEntry>().AddRange(AdministrationAuditCollector.Collect(ChangeTracker, _actorProvider?.ActorId ?? ICurrentActorProvider.SystemActor, Guid.Empty, now));
            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                var entityId = entry.Property("Id").CurrentValue?.ToString() ?? "(unknown)";

                if (_logger is not null)
                    PersistenceLogDefinitions.LogConcurrencyConflict(_logger, entry.Entity.GetType().Name, entityId);
                _ = await entry.GetDatabaseValuesAsync(cancellationToken).ConfigureAwait(false);
            }
            throw;
        }
        catch (DbUpdateException ex)
        {
            foreach (var entry in ex.Entries)
                if (_logger is not null)
                    PersistenceLogDefinitions.LogDatabaseError(_logger, entry.Entity.GetType().Name, ex);
            throw;
        }
        catch (Exception ex)
        {
            if (_logger is not null)
                PersistenceLogDefinitions.LogDBContextSaveChangesError(_logger, nameof(ControlPlaneDBContext), ex);
            throw;
        }
    }
}
