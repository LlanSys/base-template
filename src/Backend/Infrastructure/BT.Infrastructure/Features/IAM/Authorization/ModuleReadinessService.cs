using BT.Application.Features.ControlPlane.Modules;
using BT.Persistence.Features.IAM.DataContext;
using BT.SharedKernel.Features.ControlPlane.Modules;
using BT.SharedKernel.Features.IAM.Permissions;
using BT.SharedKernel.Features.IAM.Menus;

using Microsoft.EntityFrameworkCore;
namespace BT.Infrastructure.Features.IAM.Authorization;

internal sealed class ModuleReadinessService(IamDBContext context) : IModuleReadinessService
{
    public async Task<bool> IsReadyAsync(string moduleKey, CancellationToken cancellationToken)
        => (await GetIssuesAsync(cancellationToken).ConfigureAwait(false)).TryGetValue(moduleKey, out var issues) && issues.Count == 0;

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetIssuesAsync(CancellationToken cancellationToken)
    {
        var permissions = await context.Permissions.AsNoTracking().Where(permission => permission.IsActive && permission.AvailableTenantId == null)
            .Select(permission => permission.Key).ToListAsync(cancellationToken).ConfigureAwait(false);

        var roles = await (from role in context.Roles.IgnoreQueryFilters().AsNoTracking()
                           join claim in context.RoleClaims.AsNoTracking() on role.Id equals claim.RoleId
                           where role.IsDefault && !role.IsPlatformRole && role.IsActive && !role.IsDeleted && claim.ClaimType == "permission"
                           select new { role.Id, role.ModuleKey, claim.ClaimValue }).ToListAsync(cancellationToken).ConfigureAwait(false);

        var menus = await context.MenuItems.AsNoTracking().Where(menu => menu.IsActive && menu.AvailableTenantId == null)
            .Select(menu => new { menu.Url, menu.RequiredPermissionKey, menu.RequiredModule }).ToListAsync(cancellationToken).ConfigureAwait(false);

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var module in ModuleCatalog.Keys)
        {
            var required = ImplementedPermissions.Keys.Where(key => string.Equals(ModuleCatalog.ForPermission(key), module, StringComparison.OrdinalIgnoreCase)).ToArray();
            var issues = new List<string>();

            if (required.Any(key => !permissions.Contains(key, StringComparer.OrdinalIgnoreCase))) issues.Add("Restore all active shipped permissions for this module.");

            if (!roles.Where(role => string.Equals(role.ModuleKey, module, StringComparison.OrdinalIgnoreCase)).GroupBy(role => role.Id)
                .Any(role => required.All(key => role.Any(claim => string.Equals(claim.ClaimValue, key, StringComparison.OrdinalIgnoreCase)))))
                issues.Add("Create an active default role containing the module's shipped permissions.");

            foreach (var route in ImplementedMenuRoutes.All.Where(route => string.Equals(route.Module, module, StringComparison.OrdinalIgnoreCase)))
                if (!menus.Any(menu => string.Equals(menu.Url, route.Url, StringComparison.OrdinalIgnoreCase)
                    && ImplementedMenuRoutes.Matches(menu.Url, menu.RequiredPermissionKey, menu.RequiredModule)))
                    issues.Add("Register an active menu for " + route.Url + " with its implemented permission and module.");
            result[module] = issues;
        }
        return result;
    }
}
