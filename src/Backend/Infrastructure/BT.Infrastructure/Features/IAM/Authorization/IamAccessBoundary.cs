using BT.Application.Features.IAM.Authorization;
using BT.Domain.Features.IAM.Users.Entities;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Features.ControlPlane.Modules;

using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.IAM.Authorization;

internal sealed class IamAccessBoundary(TimeProvider timeProvider, IamDBContext context, IEffectiveAccessService accessService)
{
    public Task<EffectiveAccess> GetActorAsync(string actorId, CancellationToken cancellationToken)
        => accessService.GetAsync(actorId, cancellationToken);

    public async Task<bool> CanEditRoleAsync(string actorId, AppRole role, CancellationToken cancellationToken)
    {
        var actor = await accessService.GetAsync(actorId, cancellationToken).ConfigureAwait(false);
        return actor.IsPlatformStaff || (!role.IsDefault && !role.IsPlatformRole && role.TenantId == actor.TenantId && actor.TenantId != Guid.Empty);
    }

    public async Task<bool> CanAccessUserAsync(string actorId, AppUser user, CancellationToken cancellationToken)
    {
        var actor = await accessService.GetAsync(actorId, cancellationToken).ConfigureAwait(false);
        return user.IsPlatformStaff == actor.IsPlatformStaff && user.TenantId == actor.TenantId && actor.Permissions.Count > 0;
    }

    public async Task<bool> CanGrantPermissionsAsync(string actorId, IEnumerable<string> permissionKeys, CancellationToken cancellationToken)
    {
        var actor = await accessService.GetAsync(actorId, cancellationToken).ConfigureAwait(false);

        if (actor.IsPlatformStaff)
        {
            var keys = permissionKeys.ToArray();
            var activeKeys = await context.Permissions.AsNoTracking().Where(permission => permission.IsActive && keys.Contains(permission.Key))
                .Select(permission => permission.Key).ToListAsync(cancellationToken).ConfigureAwait(false);

            return keys.All(key => activeKeys.Contains(key, StringComparer.OrdinalIgnoreCase));
        }
        var requested = permissionKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        if (actor.Permissions.Count == 0 || !requested.All(actor.Permissions.Contains)) return false;
        var delegatable = await context.Permissions.AsNoTracking()
            .Where(permission => permission.IsActive && permission.IsTenantDelegatable && requested.Contains(permission.Key))
            .Select(permission => permission.Key).ToListAsync(cancellationToken).ConfigureAwait(false);

        return requested.All(key => delegatable.Contains(key, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<bool> CanAssignRolesAsync(string actorId, IEnumerable<string> names, CancellationToken cancellationToken)
    {
        var actor = await accessService.GetAsync(actorId, cancellationToken).ConfigureAwait(false);
        var normalized = names.Select(name => name.Trim().ToUpperInvariant()).Distinct().ToArray();
        var roles = await context.Roles.IgnoreQueryFilters().AsNoTracking()
            .Where(role => !role.IsDeleted && role.IsActive && role.IsPlatformRole == actor.IsPlatformStaff
                && (role.TenantId == actor.TenantId || role.IsDefault) && normalized.Contains(role.NormalizedName!))
            .Select(role => role.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

        if (roles.Count != normalized.Length || actor.Permissions.Count == 0) return false;
        var keys = await context.RoleClaims.AsNoTracking().Where(claim => roles.Contains(claim.RoleId)
            && claim.ClaimType == "permission" && claim.ClaimValue != null)
            .Select(claim => claim.ClaimValue!).ToListAsync(cancellationToken).ConfigureAwait(false);

        if (!actor.IsPlatformStaff && keys.Any(ModuleCatalog.IsPlatformPermission)) return false;

        if (actor.IsPlatformStaff) return keys.All(actor.Permissions.Contains);
        var keyArray = keys.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var catalog = await context.Permissions.AsNoTracking().Where(permission => permission.IsActive && keyArray.Contains(permission.Key))
            .Select(permission => new { permission.Key, permission.RequiredModule }).ToListAsync(cancellationToken).ConfigureAwait(false);

        if (catalog.Count != keyArray.Length) return false;
        var availableKeys = catalog.Where(permission =>
                (ModuleCatalog.ForPermission(permission.Key) is not { } required || actor.Modules.Contains(required))
                && (permission.RequiredModule is not { } module || actor.Modules.Contains(module)))
            .Select(permission => permission.Key);

        return await CanGrantPermissionsAsync(actorId, availableKeys, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetRolePermissionsAsync(AppRole role, IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        var desired = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existing = await context.RoleClaims.Where(claim => claim.RoleId == role.Id && claim.ClaimType == "permission")
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        context.RoleClaims.RemoveRange(existing.Where(claim => claim.ClaimValue is null || !desired.Contains(claim.ClaimValue)));
        var previous = existing.Select(claim => claim.ClaimValue).ToHashSet(StringComparer.OrdinalIgnoreCase);
        context.RoleClaims.AddRange(desired.Where(key => !previous.Contains(key)).Select(key => new Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>
        { RoleId = role.Id, ClaimType = "permission", ClaimValue = key }));

        role.ConcurrencyStamp = Guid.CreateVersion7().ToString();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetUserPermissionsAsync(AppUser user, IReadOnlyCollection<string> keys, string actorId, CancellationToken cancellationToken)
    {
        var desired = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existing = await context.UserClaims.Where(claim => claim.UserId == user.Id && claim.ClaimType == "permission")
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        context.UserClaims.RemoveRange(existing.Where(claim => claim.ClaimValue is null || !desired.Contains(claim.ClaimValue)));
        var previous = existing.Select(claim => claim.ClaimValue).ToHashSet(StringComparer.OrdinalIgnoreCase);
        context.UserClaims.AddRange(desired.Where(key => !previous.Contains(key)).Select(key => new Microsoft.AspNetCore.Identity.IdentityUserClaim<string>
        { UserId = user.Id, ClaimType = "permission", ClaimValue = key }));

        user.ConcurrencyStamp = Guid.CreateVersion7().ToString();
        user.UpdatedAt = timeProvider.GetUtcNow();
        user.UpdatedBy = actorId;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> SetUserRolesAsync(AppUser user, IReadOnlyCollection<string> names, string actorId, CancellationToken cancellationToken)
    {
        var normalized = names.Select(name => name.Trim().ToUpperInvariant()).Distinct().ToArray();
        var desired = await context.Roles.IgnoreQueryFilters().Where(role => !role.IsDeleted && role.IsActive && role.IsPlatformRole == user.IsPlatformStaff
                && (role.IsDefault || role.TenantId == user.TenantId) && normalized.Contains(role.NormalizedName!))
            .Select(role => role.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

        if (desired.Count != normalized.Length) return false;
        var existing = await context.UserRoles.Where(assignment => assignment.UserId == user.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var desiredIds = desired.ToHashSet(StringComparer.Ordinal);
        var previous = existing.Select(assignment => assignment.RoleId).ToHashSet(StringComparer.Ordinal);
        context.UserRoles.RemoveRange(existing.Where(assignment => !desiredIds.Contains(assignment.RoleId)));
        context.UserRoles.AddRange(desired.Where(id => !previous.Contains(id)).Select(id => new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
        { UserId = user.Id, RoleId = id }));

        user.ConcurrencyStamp = Guid.CreateVersion7().ToString();
        user.UpdatedAt = timeProvider.GetUtcNow();
        user.UpdatedBy = actorId;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
