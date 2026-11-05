using BT.Application.Features.IAM.Authorization;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Features.ControlPlane.Modules;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.IAM.Authorization;

/// <summary>Request-scoped access, rebuilt from current data rather than stale JWT permission claims.</summary>
internal sealed class EffectiveAccessService(IamDBContext iam, ControlPlaneDBContext controlPlane, TimeProvider timeProvider) : IEffectiveAccessService
{
    private readonly Dictionary<string, EffectiveAccess> _access = new(StringComparer.Ordinal);

    public async Task<EffectiveAccess> GetAsync(string userId, CancellationToken cancellationToken)
    {
        if (_access.TryGetValue(userId, out var cached)) return cached;

        var user = await iam.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(user => user.Id == userId && user.IsActive && !user.IsDeleted)
            .Select(user => new { user.TenantId, user.IsPlatformStaff, user.OnboardingInvitationId, user.OnboardingCompletedAt })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (user is null) return EffectiveAccess.Denied;

        if (user.OnboardingInvitationId.HasValue && (user.OnboardingCompletedAt == null
            || !await controlPlane.AdministratorInvitations.AnyAsync(item => item.Id == user.OnboardingInvitationId
                && item.ClaimedUserId == userId && item.TenantId == user.TenantId && item.AcceptanceStatus == InvitationAcceptanceStatus.Accepted, cancellationToken).ConfigureAwait(false)))
            return new EffectiveAccess(user.TenantId, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!user.IsPlatformStaff)
        {
            var active = await controlPlane.Tenants.AnyAsync(tenant => tenant.Id == user.TenantId
                && tenant.Status == TenantStatus.Active, cancellationToken).ConfigureAwait(false);

            if (!active) return EffectiveAccess.Denied;

            var now = timeProvider.GetUtcNow();
            var granted = await controlPlane.TenantModules.AsNoTracking()
                .Where(module => module.TenantId == user.TenantId && module.IsActive
                    && (module.ExpiresAt == null || module.ExpiresAt > now))
                .Select(module => module.ModuleKey).ToListAsync(cancellationToken).ConfigureAwait(false);

            var enabled = await controlPlane.Modules.AsNoTracking().Where(module => module.IsActive)
                .Select(module => module.Key).ToListAsync(cancellationToken).ConfigureAwait(false);

            modules.UnionWith(enabled.Where(key => ModuleCatalog.IsImplemented(key) && granted.Contains(key, StringComparer.OrdinalIgnoreCase)));
        }

        var roleIds = from assignment in iam.UserRoles.AsNoTracking()
                      join role in iam.Roles.IgnoreQueryFilters().AsNoTracking() on assignment.RoleId equals role.Id
                      where assignment.UserId == userId && !role.IsDeleted && role.IsActive
                          && role.IsPlatformRole == user.IsPlatformStaff
                          && (role.TenantId == user.TenantId || role.IsDefault)
                      select role.Id;

        var roleKeys = await iam.RoleClaims.AsNoTracking()
            .Where(claim => roleIds.Contains(claim.RoleId) && claim.ClaimType == "permission" && claim.ClaimValue != null)
            .Select(claim => claim.ClaimValue!).ToListAsync(cancellationToken).ConfigureAwait(false);

        var directKeys = await iam.UserClaims.AsNoTracking()
            .Where(claim => claim.UserId == userId && claim.ClaimType == "permission" && claim.ClaimValue != null)
            .Select(claim => claim.ClaimValue!).ToListAsync(cancellationToken).ConfigureAwait(false);

        var grantedKeys = roleKeys.Concat(directKeys).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var grantedKeyArray = grantedKeys.ToArray();
        var catalog = await iam.Permissions.AsNoTracking().Where(permission => permission.IsActive && grantedKeyArray.Contains(permission.Key)
                && (permission.AvailableTenantId == null || permission.AvailableTenantId == user.TenantId))
            .Select(permission => new { permission.Key, permission.RequiredModule, permission.Context }).ToListAsync(cancellationToken).ConfigureAwait(false);

        var permissions = catalog.Where(permission => grantedKeys.Contains(permission.Key)
            && (user.IsPlatformStaff ? !permission.Key.StartsWith("tenant-settings.", StringComparison.OrdinalIgnoreCase)
                : !ModuleCatalog.IsPlatformPermission(permission.Key) && !ModuleCatalog.IsPlatformContext(permission.Context))
            && (ModuleCatalog.ForPermission(permission.Key) is not { } requiredModule || (!user.IsPlatformStaff && modules.Contains(requiredModule)))
            && (permission.RequiredModule is not { } module || (!user.IsPlatformStaff && modules.Contains(module))))
            .Select(permission => permission.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new EffectiveAccess(user.TenantId, user.IsPlatformStaff, permissions, modules);
        _access[userId] = result;
        return result;
    }
}
