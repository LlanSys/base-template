using System.Collections.Frozen;

namespace BT.SharedKernel.Features.ControlPlane.Modules;

/// <summary>Capabilities implemented by this release. Catalog records cannot manufacture functionality.</summary>
public static class ModuleCatalog
{
    private static readonly FrozenDictionary<string, string> PermissionModules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["customers"] = "Banking",
        ["departments"] = "HR",
        ["employees"] = "HR",
        ["payments"] = "Payments"
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> AdministrativePermissions = new[]
    {
        "users.create", "users.edit", "users.deactivate", "users.manage_roles", "users.manage_permissions",
        "roles.create", "roles.edit", "roles.delete", "roles.manage_permissions"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Keys { get; } = Array.AsReadOnly(new[] { "Banking", "HR", "Payments" });

    public static bool IsImplemented(string key) => Keys.Contains(key, StringComparer.OrdinalIgnoreCase);

    public static string? ForPermission(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var separator = key.IndexOf('.', StringComparison.Ordinal);
        var resource = separator < 0 ? key : key[..separator];
        return PermissionModules.GetValueOrDefault(resource);
    }

    public static bool IsPlatformPermission(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key.StartsWith("controlplane.", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("modules.", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("menus.", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("permissions.", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPlatformContext(string context) => string.Equals(context, "Platform", StringComparison.OrdinalIgnoreCase)
        || string.Equals(context, "ControlPlane", StringComparison.OrdinalIgnoreCase);

    public static bool IsAdministrativePermission(string key) => IsPlatformPermission(key)
        || AdministrativePermissions.Contains(key);
}
