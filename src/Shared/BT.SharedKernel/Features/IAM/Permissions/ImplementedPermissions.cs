using System.Collections.Frozen;
namespace BT.SharedKernel.Features.IAM.Permissions;

/// <summary>Permission contracts used by shipped operations. Retirement requires a code release.</summary>
public static class ImplementedPermissions
{
    public static IReadOnlySet<string> Keys
    {
        get;
    } = new[]
    {
        "controlplane.create_tenants",
        "controlplane.delete_tenants",
        "controlplane.edit_tenants",
        "controlplane.impersonate",
        "controlplane.manage_stamps",
        "controlplane.manage_tenant_administrators",
        "controlplane.manage_tenant_lifecycle",
        "controlplane.manage_tenant_modules",
        "controlplane.migrate_tenants",
        "controlplane.view_audit",
        "controlplane.view_stamps",
        "controlplane.view_tenants",
        "customers.create",
        "customers.delete",
        "customers.edit",
        "customers.view",
        "departments.create",
        "departments.delete",
        "departments.edit",
        "departments.view",
        "employees.create",
        "employees.delete",
        "employees.edit",
        "employees.view",
        "menus.create",
        "menus.delete",
        "menus.edit",
        "menus.view",
        "modules.create",
        "modules.manage",
        "modules.view",
        "payments.admin",
        "payments.create",
        "payments.view",
        "permissions.manage",
        "roles.create",
        "roles.delete",
        "roles.edit",
        "roles.manage_permissions",
        "roles.view",
        "tenant-settings.update",
        "tenant-settings.view",
        "users.create",
        "users.deactivate",
        "users.edit",
        "users.manage_permissions",
        "users.manage_roles",
        "users.view",
    }
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsProtected(string key)
    {
        return Keys.Contains(key);
    }

    public static bool IsRetired(string key)
    {
        return string.Equals(key, "controlplane.manage", StringComparison.OrdinalIgnoreCase);
    }
}
