using BT.Domain.Features.IAM.Menus.Entities;

namespace BT.Persistence.Features.IAM.Menus.Seeds;

internal static class MenuItemSeed
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid HrId = Guid.Parse("018fd81d-2c94-7ad0-a4a3-f1edb9c20401");
    internal static IReadOnlyList<MenuItem> Items =>
    [
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20101", null, "banking", "Banking", "Customer management.", "/admin/customers", "Business", "customers.view", "Banking", 10),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20401", null, "hr", "Human Resources", "Staff and departments.", "", "Badge", null, "HR", 20),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20102", HrId, "departments", "Departments", "Department catalog.", "/admin/departments", "AccountTree", "departments.view", "HR", 10),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c20103", HrId, "employees", "Employees", "Staff records and access.", "/admin/employees", "Badge", "employees.view", "HR", 20),
        Create("018fd81d-2c94-7ad0-a4a3-f1edb9c10402", null, "payments", "Payments", "Payments and provider operations.", "/payments", "CreditCard", "payments.view", "Payments", 30)
    ];

    private static MenuItem Create(string id, Guid? parent, string key, string title, string description, string url,
        string icon, string? permission, string module, int order)
    {
        var item = MenuItem.Create(parent, null, key, title, description, url, icon, "Sidebar", permission, module, order,
            "00000000-0000-0000-0000-000000000001");

        item.Id = Guid.Parse(id);
        item.CreatedAt = CreatedAt;
        return item;
    }
}
