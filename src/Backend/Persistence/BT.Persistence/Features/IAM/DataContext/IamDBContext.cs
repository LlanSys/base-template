using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Domain.Features.IAM.Menus.Entities;
using BT.Domain.Features.IAM.Permissions.Entities;
using BT.Domain.Features.IAM.ReferenceData.Entities;
using BT.Domain.Shared.Entities;
using BT.Persistence.Common;
using BT.Persistence.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BT.Persistence.Features.IAM.DataContext;

public class IamDBContext : IdentityDbContext<AppUser, AppRole, string>, ITenantFilteredDBContext
{
    private readonly ICurrentTenantProvider? _tenantProvider;
    private readonly ICurrentActorProvider? _actorProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IamDBContext>? _logger;

    public IamDBContext(
        DbContextOptions<IamDBContext> options,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<IamDBContext>? logger = null,
        TimeProvider? timeProvider = null
    ) : base(options)
    {
        _tenantProvider = tenantProvider;
        _actorProvider = actorProvider;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected IamDBContext(
        DbContextOptions options,
        ICurrentTenantProvider? tenantProvider = null,
        ICurrentActorProvider? actorProvider = null,
        ILogger<IamDBContext>? logger = null,
        TimeProvider? timeProvider = null
    ) : base(options)
    {
        _tenantProvider = tenantProvider;
        _actorProvider = actorProvider;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }
    public DbSet<AppUserProfile> AppUserProfiles { get; set; }
    public DbSet<AppUserTotpSecret> AppUserTotpSecrets { get; set; }
    public DbSet<AppUserSession> AppUserSessions { get; set; }
    public DbSet<AppUserDevice> AppUserDevices { get; set; }
    public DbSet<Fido2Credential> Fido2Credentials { get; set; }
    public DbSet<TempTotpSecret> TempTotpSecrets { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<PermissionContext> PermissionContexts { get; set; }
    public DbSet<PermissionResource> PermissionResources { get; set; }
    public DbSet<PermissionAction> PermissionActions { get; set; }
    public DbSet<MenuPlacement> MenuPlacements { get; set; }
    public DbSet<MenuIcon> MenuIcons { get; set; }
    public DbSet<MenuRoute> MenuRoutes { get; set; }
    public Guid CurrentTenantId => _tenantProvider?.TenantId ?? Guid.Empty;

    private List<IDomainEvent> _collectedDomainEvents = [];
    public IReadOnlyList<IDomainEvent>? GetCollectedDomainEvents() => _collectedDomainEvents?.AsReadOnly();
    public void ClearCollectedDomainEvents() => _collectedDomainEvents?.Clear();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(IamDBContext).Assembly,
            type => type.Namespace?.StartsWith("BT.Persistence.Features.IAM", StringComparison.Ordinal) == true &&
                    !(type.Namespace?.Contains("SqlServer") == true) &&
                    !(type.Namespace?.Contains("PostgreSql") == true));

        DBContextHelper.ApplyStandardModelConventions(builder, this);

        builder.Entity<AppUser>(entity => entity.ToTable("Users"));
        builder.Entity<AppRole>(entity => entity.ToTable("Roles"));
        builder.Entity<AppRole>().HasQueryFilter(role => !role.IsDeleted && (role.IsDefault || role.TenantId == CurrentTenantId));
        builder.Entity<AdministrationAuditEntry>().HasQueryFilter(entry => entry.TenantId == CurrentTenantId);
        builder.Entity<IdentityUserRole<string>>(entity => entity.ToTable("UserRoles"));
        builder.Entity<IdentityRoleClaim<string>>(entity => entity.ToTable("RoleClaims"));
        builder.Entity<IdentityUserClaim<string>>(entity => entity.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<string>>(entity => entity.ToTable("UserLogins"));
        builder.Entity<IdentityUserToken<string>>(entity => entity.ToTable("UserTokens"));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var domainEvents = DBContextHelper.CollectDomainEvents(ChangeTracker);
            DBContextHelper.ClearDomainEventsFromAggregates(ChangeTracker);
            var now = _timeProvider.GetUtcNow();
            DBContextHelper.UpdateAuditAndSoftDelete(ChangeTracker, _actorProvider?.ActorId ?? ICurrentActorProvider.SystemActor, CurrentTenantId, now);

            foreach (var entry in ChangeTracker.Entries<AppUser>())
            {
                if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = now;
            }

            Set<AdministrationAuditEntry>().AddRange(AdministrationAuditCollector.Collect(ChangeTracker, _actorProvider?.ActorId ?? ICurrentActorProvider.SystemActor, CurrentTenantId, now));

            var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _collectedDomainEvents ??= [];
            _collectedDomainEvents.AddRange(domainEvents);

            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                var entityId = entry.Entity is BaseEntity b ? b.Id.ToString() : "(unknown)";

                if (_logger is not null)
                    PersistenceLogDefinitions.LogConcurrencyConflict(_logger, entry.Entity.GetType().Name, entityId);
                _ = await entry.GetDatabaseValuesAsync(cancellationToken).ConfigureAwait(false);
            }
            _collectedDomainEvents?.Clear();
            throw;
        }
        catch (DbUpdateException ex)
        {
            foreach (var entry in ex.Entries)
                if (_logger is not null)
                    PersistenceLogDefinitions.LogDatabaseError(_logger, entry.Entity.GetType().Name, ex);
            _collectedDomainEvents?.Clear();
            throw;
        }
        catch (Exception ex)
        {
            if (_logger is not null)
                PersistenceLogDefinitions.LogDBContextSaveChangesError(_logger, nameof(IamDBContext), ex);
            _collectedDomainEvents?.Clear();
            throw;
        }
    }
}
