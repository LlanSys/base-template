using System.Security.Claims;

using BT.Domain.Features.IAM.Users.Entities;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Features.ControlPlane.Modules;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.IAM.Authorization;

public sealed class PlatformCatalogInitializer(RoleManager<AppRole> roles, IamDBContext context)
{
    public Task InitializeAsync(CancellationToken cancellationToken)
        => IdentityWriteScope.RunAsync(context, async () =>
        {
            await InitializeCoreAsync(cancellationToken).ConfigureAwait(false);
            return BT.SharedKernel.Dtos.Common.AppResponses.Success(true);
        }, cancellationToken, System.Data.IsolationLevel.Serializable);

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        await MigrateLegacyRolesAsync(cancellationToken).ConfigureAwait(false);
        var catalog = await context.Permissions.AsNoTracking().Where(permission => permission.IsActive)
            .Select(permission => new { permission.Key, permission.Context }).ToListAsync(cancellationToken).ConfigureAwait(false);

        var keys = catalog.Select(permission => permission.Key).ToList();
        var platformKeys = catalog.Where(permission => ModuleCatalog.IsPlatformContext(permission.Context))
            .Select(permission => permission.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in new[]
        {
            new DefaultRole("System Administrator", true, null),
            new DefaultRole("Tenant Administrator", false, null),
            new DefaultRole("Banking Administrator", false, "Banking"),
            new DefaultRole("HR Administrator", false, "HR"),
            new DefaultRole("Payments Administrator", false, "Payments"),
            new DefaultRole("Employee", false, null),
            new DefaultRole("Customer", false, null)
        })
        {
            var normalized = definition.Name.ToUpperInvariant();
            var legacyDefault = definition.Name is "System Administrator" or "Employee" or "Customer";
            var role = await roles.Roles.IgnoreQueryFilters().SingleOrDefaultAsync(
                role => role.DefinitionKey == definition.Key || legacyDefault && role.NormalizedName == normalized && role.TenantId == Guid.Empty,
                cancellationToken).ConfigureAwait(false);

            if (role is { IsDefault: true }) continue;
            var isNew = role is null;

            if (role is null)
            {
                var name = definition.Name;
                var candidateNormalizedName = normalized;
                var suffix = 0;

                while (await context.Roles.IgnoreQueryFilters().AnyAsync(existing => existing.NormalizedName == candidateNormalizedName, cancellationToken).ConfigureAwait(false))
                {
                    name = definition.Name + " (default " + (++suffix).ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                    candidateNormalizedName = name.ToUpperInvariant();
                }
                role = new AppRole { Name = name };
            }
            role.DefinitionKey = definition.Key;
            role.IsDefault = true;
            role.IsPlatformRole = definition.Platform;
            role.ModuleKey = definition.Module;
            EnsureSucceeded(isNew ? await roles.CreateAsync(role).ConfigureAwait(false) : await roles.UpdateAsync(role).ConfigureAwait(false));
            var existing = (await roles.GetClaimsAsync(role).ConfigureAwait(false))
                .Where(claim => claim.Type == "permission").Select(claim => claim.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var grants = keys.Where(key => definition.Platform ? ModuleCatalog.ForPermission(key) is null
                : definition.Module is not null ? string.Equals(ModuleCatalog.ForPermission(key), definition.Module, StringComparison.OrdinalIgnoreCase)
                : definition.Name == "Tenant Administrator" && !ModuleCatalog.IsPlatformPermission(key) && !platformKeys.Contains(key));

            foreach (var key in grants.Where(key => !existing.Contains(key)))
                EnsureSucceeded(await roles.AddClaimAsync(role, new Claim("permission", key)).ConfigureAwait(false));

            if (definition.Platform)
            {
                var userIds = context.UserRoles.Where(assignment => assignment.RoleId == role.Id).Select(assignment => assignment.UserId);
                var legacyAdministrators = await context.Users.IgnoreQueryFilters().Where(user => userIds.Contains(user.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);

                foreach (var user in legacyAdministrators) user.IsPlatformStaff = true;
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task MigrateLegacyRolesAsync(CancellationToken cancellationToken)
    {
        var reserved = new[] { "SYSTEM ADMINISTRATOR", "EMPLOYEE", "CUSTOMER" };
        var legacy = await context.Roles.IgnoreQueryFilters().Where(role => role.TenantId == Guid.Empty && !role.IsDefault
            && !reserved.Contains(role.NormalizedName!)).OrderBy(role => role.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

        if (legacy.Count == 0) return;
        var ids = legacy.Select(role => role.Id).ToArray();
        var assignments = await context.UserRoles.Where(assignment => ids.Contains(assignment.RoleId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var userIds = assignments.Select(assignment => assignment.UserId).ToArray();
        var tenants = await context.Users.IgnoreQueryFilters().Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.TenantId, cancellationToken).ConfigureAwait(false);

        var claims = await context.RoleClaims.Where(claim => ids.Contains(claim.RoleId)).ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var role in legacy)
        {
            var groups = assignments.Where(assignment => assignment.RoleId == role.Id)
                .GroupBy(assignment => tenants[assignment.UserId]).OrderBy(group => group.Key).ToArray();

            if (groups.Length == 0) { role.IsActive = false; continue; }
            role.TenantId = groups[0].Key;

            foreach (var group in groups.Skip(1))
            {
                var copy = new AppRole
                {
                    Name = role.Name,
                    NormalizedName = role.NormalizedName,
                    TenantId = group.Key,
                    DepartmentId = role.DepartmentId,
                    IsActive = role.IsActive
                };

                context.Roles.Add(copy);

                foreach (var claim in claims.Where(claim => claim.RoleId == role.Id))
                    context.RoleClaims.Add(new IdentityRoleClaim<string> { RoleId = copy.Id, ClaimType = claim.ClaimType, ClaimValue = claim.ClaimValue });

                foreach (var assignment in group)
                {
                    context.UserRoles.Remove(assignment);
                    context.UserRoles.Add(new IdentityUserRole<string> { UserId = assignment.UserId, RoleId = copy.Id });
                }
            }
        }
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Platform role initialization failed. Check identity configuration.");
    }

    private sealed record DefaultRole(string Name, bool Platform, string? Module)
    {
        public string Key => Name == "System Administrator" ? "platform-administrator" : Name.ToLowerInvariant().Replace(' ', '-');
    }
}
