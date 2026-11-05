using System.Collections.Frozen;

namespace BT.SharedKernel.Features.ControlPlane;

public static class PlatformPermissions
{
    public static IReadOnlyList<string> Operations { get; } = Array.AsReadOnly(new[]
    {
        "controlplane.view_tenants", "controlplane.create_tenants", "controlplane.edit_tenants", "controlplane.delete_tenants",
        "controlplane.manage_tenant_modules", "controlplane.manage_tenant_administrators", "controlplane.manage_tenant_lifecycle",
        "controlplane.migrate_tenants", "controlplane.view_stamps", "controlplane.manage_stamps", "controlplane.view_audit",
        "controlplane.impersonate", "modules.view"
    });

    private static readonly FrozenSet<string> ShellPermissions = Operations.Concat(new[]
    {
        "modules.create", "modules.manage", "menus.view", "menus.create", "menus.edit", "menus.delete", "permissions.manage",
        "roles.view", "roles.create", "roles.edit", "roles.delete", "roles.manage_permissions",
        "users.view", "users.create", "users.edit", "users.deactivate", "users.manage_roles", "users.manage_permissions"
    }).ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool OpensControlPanel(IEnumerable<string> permissions) => permissions.Any(ShellPermissions.Contains);
}
