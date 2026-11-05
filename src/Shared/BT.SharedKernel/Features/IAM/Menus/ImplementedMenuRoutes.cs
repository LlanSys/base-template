using System.Collections.Frozen;
namespace BT.SharedKernel.Features.IAM.Menus;

public static class ImplementedMenuRoutes
{
    private static readonly FrozenDictionary<string, ImplementedMenuRoute> Routes = new[]
    {
        new ImplementedMenuRoute("/admin/customers", "customers.view", "Banking"),
        new ImplementedMenuRoute("/admin/departments", "departments.view", "HR"),
        new ImplementedMenuRoute("/admin/employees", "employees.view", "HR"),
        new ImplementedMenuRoute("/payments", "payments.view", "Payments")
    }.ToFrozenDictionary(route => route.Url, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<ImplementedMenuRoute> All => Routes.Values;

    public static bool Matches(string url, string? permission, string? module) => string.IsNullOrWhiteSpace(url)
        ? string.IsNullOrWhiteSpace(module) || BT.SharedKernel.Features.ControlPlane.Modules.ModuleCatalog.IsImplemented(module)
        : Routes.TryGetValue(url, out var route) && string.Equals(route.Permission, permission, StringComparison.OrdinalIgnoreCase)
            && string.Equals(route.Module, module, StringComparison.OrdinalIgnoreCase);
}
