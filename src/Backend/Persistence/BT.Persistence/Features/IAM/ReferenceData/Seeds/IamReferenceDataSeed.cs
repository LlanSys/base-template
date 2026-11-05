using BT.Domain.Features.IAM.ReferenceData.Entities;

namespace BT.Persistence.Features.IAM.ReferenceData.Seeds;

internal static class IamReferenceDataSeed
{
    private static readonly DateTimeOffset SeedCreatedAt = new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    private static readonly Guid SeedTenantId = new("0194f700-0000-7000-8000-000000000001");

    internal static IReadOnlyList<PermissionContext> PermissionContexts =>
    [
        CreateContext("018fd81d-2c94-7ad0-a4a3-f1edb9d10101", "Banking", "Banking", "Customer, accounts, loans, and financial operations."),
        CreateContext("018fd81d-2c94-7ad0-a4a3-f1edb9d10102", "HR", "Human Resources", "Departments, employees, and staff operations."),
        CreateContext("018fd81d-2c94-7ad0-a4a3-f1edb9d10103", "IAM", "Identity and Access", "Users, roles, permissions, sessions, and devices."),
        CreateContext("018fd81d-2c94-7ad0-a4a3-f1edb9d10104", "Platform", "Platform", "Cross-cutting platform configuration and navigation."),
        CreateContext("018fd81d-2c94-7ad0-a4a3-f1edb9d10105", "Shared", "Shared", "Cross-cutting application services."),
        CreateContext("018fd81d-2c94-7ad0-a4a3-f1edb9d10106", "ControlPlane", "Control Plane", "Vendor administration.")
    ];

    internal static IReadOnlyList<PermissionResource> PermissionResources =>
    [
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10201", "customers", "Customers", "Banking", "Customer records and onboarding."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10202", "departments", "Departments", "HR", "Department catalog and staff grouping."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10203", "employees", "Employees", "HR", "Employee records and IAM linkage."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10204", "menus", "Menus", "Platform", "Navigation registry and menu visibility."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10205", "permissions", "Permissions", "Platform", "Permission catalog and assignment surface."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10206", "roles", "Roles", "IAM", "Role catalog and permission bundles."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10207", "users", "Users", "IAM", "Application user accounts."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10208", "payments", "Payments", "Shared", "Payment checkout, status, and provider administration."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10209", "modules", "Modules", "ControlPlane", "Business module definitions."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10210", "controlplane", "Control Plane", "ControlPlane", "Vendor operations."),
        CreateResource("018fd81d-2c94-7ad0-a4a3-f1edb9d10211", "tenant-settings", "Organization Settings", "Shared", "Supported organization preferences.")
    ];

    internal static IReadOnlyList<PermissionAction> PermissionActions =>
    [
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10301", "view", "View", "Read and list records."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10302", "create", "Create", "Create new records."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10303", "edit", "Edit", "Update existing records."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10304", "delete", "Delete", "Soft-delete or remove records."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10305", "deactivate", "Deactivate", "Disable active records or accounts."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10306", "manage_permissions", "Manage permissions", "Assign or revoke permissions."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10307", "manage_roles", "Manage roles", "Assign or revoke roles."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10308", "admin", "Administer", "Perform restricted provider administration actions."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10309", "update", "Update", "Update organization preferences."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10310", "manage", "Manage", "Manage vendor catalogs."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10311", "create_tenants", "Create tenants", "Provision tenant records."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10312", "view_tenants", "View tenants", "Vendor operation."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10313", "edit_tenants", "Edit tenants", "Vendor operation."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10314", "delete_tenants", "Delete tenants", "Vendor operation."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10315", "manage_tenant_modules", "Manage tenant modules", "Vendor operation."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10316", "manage_tenant_administrators", "Manage tenant administrators", "Vendor operation."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10317", "manage_tenant_lifecycle", "Manage tenant lifecycle", "Vendor operation."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10318", "migrate_tenants", "Migrate tenants", "Vendor operation."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10319", "view_stamps", "View stamps", "Vendor operation."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10320", "manage_stamps", "Manage stamps", "Vendor operation."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10321", "view_audit", "View audit", "Vendor operation."),
        CreateAction("018fd81d-2c94-7ad0-a4a3-f1edb9d10322", "impersonate", "Impersonate", "Vendor operation.")
    ];

    internal static IReadOnlyList<MenuPlacement> MenuPlacements =>
    [
        CreatePlacement("018fd81d-2c94-7ad0-a4a3-f1edb9d10401", "Sidebar", "Sidebar", "Main application navigation."),
        CreatePlacement("018fd81d-2c94-7ad0-a4a3-f1edb9d10402", "AdminCenter", "Admin Center", "Administration landing tiles."),
        CreatePlacement("018fd81d-2c94-7ad0-a4a3-f1edb9d10403", "ControlPanel", "Control Panel", "Platform control plane tiles.")
    ];

    internal static IReadOnlyList<MenuIcon> MenuIcons =>
    [
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10501", "AccountTree", "Account tree"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10502", "AdminPanelSettings", "Admin panel"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10503", "AutoStories", "Story/book"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10504", "Badge", "Badge"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10505", "Business", "Business"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10506", "Dashboard", "Dashboard"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10507", "Devices", "Devices"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10508", "Group", "Group"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10509", "LockPerson", "Security lock"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10510", "Menu", "Generic menu"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10511", "MenuOpen", "Menu"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10512", "Settings", "Settings"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10513", "CreditCard", "Credit card"),
        CreateIcon("018fd81d-2c94-7ad0-a4a3-f1edb9d10514", "Dns", "DNS/platform")
    ];

    internal static IReadOnlyList<MenuRoute> MenuRoutes =>
    [
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10604", "customers", "Customers", "/admin/customers", "Sidebar"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10605", "departments", "Departments", "/admin/departments", "Sidebar"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10606", "employees", "Employees", "/admin/employees", "Sidebar"),
        CreateRoute("018fd81d-2c94-7ad0-a4a3-f1edb9d10615", "payments", "Payments", "/payments", "Sidebar")
    ];

    private static PermissionContext CreateContext(string id, string key, string label, string description)
    {
        var item = PermissionContext.Create(key, label, description, "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }

    private static PermissionResource CreateResource(string id, string key, string label, string contextKey, string description)
    {
        var item = PermissionResource.Create(key, label, contextKey, description, "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }

    private static PermissionAction CreateAction(string id, string key, string label, string description)
    {
        var item = PermissionAction.Create(key, label, description, "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }

    private static MenuPlacement CreatePlacement(string id, string key, string label, string description)
    {
        var item = MenuPlacement.Create(key, label, description, "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }

    private static MenuIcon CreateIcon(string id, string key, string label)
    {
        var item = MenuIcon.Create(key, label, "Approved MudBlazor icon key.", "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }

    private static MenuRoute CreateRoute(string id, string key, string label, string url, string placementKey)
    {
        var item = MenuRoute.Create(key, label, url, placementKey, "Approved application route.", "System");
        item.Id = Guid.Parse(id);
        item.TenantId = SeedTenantId;
        item.CreatedAt = SeedCreatedAt;
        return item;
    }
}
