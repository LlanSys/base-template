using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BT.Persistence.Features.IAM.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class SaasAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve custom children before retiring the seeded demo and foundation menus.
            migrationBuilder.Sql("""
                UPDATE [MenuItems] SET [ParentId] = NULL WHERE [ParentId] IN ('018fd81d-2c94-7ad0-a4a3-f1edb9c10101', '018fd81d-2c94-7ad0-a4a3-f1edb9c10301', '018fd81d-2c94-7ad0-a4a3-f1edb9c10401', '018fd81d-2c94-7ad0-a4a3-f1edb9c20104', '018fd81d-2c94-7ad0-a4a3-f1edb9c20107', '018fd81d-2c94-7ad0-a4a3-f1edb9c20110', '018fd81d-2c94-7ad0-a4a3-f1edb9c20111', '018fd81d-2c94-7ad0-a4a3-f1edb9c30101', '018fd81d-2c94-7ad0-a4a3-f1edb9c30102', '018fd81d-2c94-7ad0-a4a3-f1edb9c10201', '018fd81d-2c94-7ad0-a4a3-f1edb9c10501');
                """);

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "Roles");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10101"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10301"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20104"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20107"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20110"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20111"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30101"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30102"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10601"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10602"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10603"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10607"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10608"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10609"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10610"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10611"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10612"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10613"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10614"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10616"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10617"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10618"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10619"));

            migrationBuilder.DeleteData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10620"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10501"));

            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformStaff",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DefinitionKey",
                table: "Roles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformRole",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModuleKey",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Roles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AvailableTenantId",
                table: "Permissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredModule",
                table: "Permissions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AvailableTenantId",
                table: "MenuItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IamAdministrationAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChangedFields = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IamAdministrationAudit", x => x.Id);
                });

            // Create the new HR parent before reparenting existing department/employee links.
            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "AvailableTenantId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DepartmentId", "Description", "DisplayOrder", "Icon", "IsActive", "IsDeleted", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "TenantId", "Title", "UpdatedAt", "UpdatedBy", "Url" },
                values: new object[] { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20401"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0000-000000000001", null, null, null, "Staff and departments.", 20, "Badge", true, false, "hr", null, "Sidebar", "hr", null, new Guid("00000000-0000-0000-0000-000000000000"), "Human Resources", null, null, "" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10402"),
                columns: new[] { "AvailableTenantId", "CreatedBy", "Description", "DisplayOrder", "Key", "ParentId", "Placement", "RequiredModule", "TenantId", "Url" },
                values: new object[] { null, "00000000-0000-0000-0000-000000000001", "Payments and provider operations.", 30, "payments", null, "Sidebar", "payments", new Guid("00000000-0000-0000-0000-000000000000"), "/payments" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20101"),
                columns: new[] { "AvailableTenantId", "CreatedBy", "Description", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "TenantId", "Title" },
                values: new object[] { null, "00000000-0000-0000-0000-000000000001", "Customer management.", "banking", null, "Sidebar", "banking", "customers.view", new Guid("00000000-0000-0000-0000-000000000000"), "Banking" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20102"),
                columns: new[] { "AvailableTenantId", "CreatedBy", "Description", "DisplayOrder", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "TenantId" },
                values: new object[] { null, "00000000-0000-0000-0000-000000000001", "Department catalog.", 10, "departments", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20401"), "Sidebar", "hr", "departments.view", new Guid("00000000-0000-0000-0000-000000000000") });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20103"),
                columns: new[] { "AvailableTenantId", "CreatedBy", "Description", "DisplayOrder", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "TenantId" },
                values: new object[] { null, "00000000-0000-0000-0000-000000000001", "Staff records and access.", 20, "employees", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20401"), "Sidebar", "hr", "employees.view", new Guid("00000000-0000-0000-0000-000000000000") });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10604"),
                columns: new[] { "Key", "PlacementKey" },
                values: new object[] { "customers", "Sidebar" });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10605"),
                columns: new[] { "Key", "PlacementKey" },
                values: new object[] { "departments", "Sidebar" });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10606"),
                columns: new[] { "Key", "PlacementKey" },
                values: new object[] { "employees", "Sidebar" });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10615"),
                columns: new[] { "Key", "Url" },
                values: new object[] { "payments", "/payments" });

            migrationBuilder.InsertData(
                table: "PermissionActions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10309"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Update organization preferences.", true, false, "update", "Update", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10310"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Manage vendor catalogs.", true, false, "manage", "Manage", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10311"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Provision tenant records.", true, false, "create_tenants", "Create tenants", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.UpdateData(
                table: "PermissionContexts",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10105"),
                column: "Description",
                value: "Cross-cutting application services.");

            migrationBuilder.InsertData(
                table: "PermissionContexts",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10106"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor administration.", true, false, "ControlPlane", "Control Plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null });

            migrationBuilder.UpdateData(
                table: "PermissionResources",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10205"),
                column: "ContextKey",
                value: "Platform");

            migrationBuilder.InsertData(
                table: "PermissionResources",
                columns: new[] { "Id", "ContextKey", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10209"), "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Business module definitions.", true, false, "modules", "Modules", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10210"), "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operations.", true, false, "controlplane", "Control Plane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10211"), "Shared", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Supported organization preferences.", true, false, "tenant-settings", "Organization Settings", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10101"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10102"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10103"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10104"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10105"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10106"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10201"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10202"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10203"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10204"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10205"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10301"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10302"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10303"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10304"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10401"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10402"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10403"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10404"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10501"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10502"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10503"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10504"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10601"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10602"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10603"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10604"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10701"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10702"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10703"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10801"),
                columns: new[] { "AvailableTenantId", "RequiredModule" },
                values: new object[] { null, null });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "AvailableTenantId", "Context", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DepartmentId", "Description", "IsActive", "IsDeleted", "Key", "RequiredModule", "Resource", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10802"), "create_tenants", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Create tenants. Platform administrators only.", true, false, "controlplane.create_tenants", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10803"), "create", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Register implemented business modules.", true, false, "modules.create", null, "modules", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10804"), "manage", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Manage module definitions and availability.", true, false, "modules.manage", null, "modules", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10805"), "manage", null, "Platform", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Manage implemented permission definitions.", true, false, "permissions.manage", null, "permissions", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10901"), "view", null, "Shared", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "View organization settings.", true, false, "tenant-settings.view", null, "tenant-settings", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10902"), "update", null, "Shared", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Change supported organization settings.", true, false, "tenant-settings.update", null, "tenant-settings", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_DefinitionKey",
                table: "Roles",
                column: "DefinitionKey",
                unique: true,
                filter: "[DefinitionKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId_NormalizedName",
                table: "Roles",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_IamAdministrationAudit_TenantId_OccurredAt",
                table: "IamAdministrationAudit",
                columns: new[] { "TenantId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IamAdministrationAudit");

            migrationBuilder.DropIndex(
                name: "IX_Roles_DefinitionKey",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId_NormalizedName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "Roles");

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20401"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10309"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10310"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10311"));

            migrationBuilder.DeleteData(
                table: "PermissionContexts",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10106"));

            migrationBuilder.DeleteData(
                table: "PermissionResources",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10209"));

            migrationBuilder.DeleteData(
                table: "PermissionResources",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10210"));

            migrationBuilder.DeleteData(
                table: "PermissionResources",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10211"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10802"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10803"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10804"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10805"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10901"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10902"));

            migrationBuilder.DropColumn(
                name: "IsPlatformStaff",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefinitionKey",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsPlatformRole",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "ModuleKey",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "AvailableTenantId",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "RequiredModule",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "AvailableTenantId",
                table: "MenuItems");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10402"),
                columns: new[] { "CreatedBy", "Description", "DisplayOrder", "Key", "ParentId", "Placement", "RequiredModule", "TenantId", "Url" },
                values: new object[] { "System", "Test card and mobile-money payment flows.", 10, "features-payments", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"), "Features", "showcase", new Guid("0194f700-0000-7000-8000-000000000001"), "/features/payments" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20101"),
                columns: new[] { "CreatedBy", "Description", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "TenantId", "Title" },
                values: new object[] { "System", "Customer records and onboarding.", "admin-customers", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "core", null, new Guid("0194f700-0000-7000-8000-000000000001"), "Customers" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20102"),
                columns: new[] { "CreatedBy", "Description", "DisplayOrder", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "TenantId" },
                values: new object[] { "System", "Department catalog and staff grouping.", 20, "admin-departments", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "core", null, new Guid("0194f700-0000-7000-8000-000000000001") });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20103"),
                columns: new[] { "CreatedBy", "Description", "DisplayOrder", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "TenantId" },
                values: new object[] { "System", "Staff records and system access.", 30, "admin-employees", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "core", null, new Guid("0194f700-0000-7000-8000-000000000001") });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DepartmentId", "Description", "DisplayOrder", "Icon", "IsActive", "IsDeleted", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "TenantId", "Title", "UpdatedAt", "UpdatedBy", "Url" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10101"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Operations dashboard.", 2, "Dashboard", true, false, "dashboard", null, "Sidebar", "core", null, new Guid("0194f700-0000-7000-8000-000000000001"), "Dashboard", null, null, "/dashboard" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Administrative workspace.", 3, "AdminPanelSettings", true, false, "admin-center", null, "Sidebar", "core", null, new Guid("0194f700-0000-7000-8000-000000000001"), "Admin Center", null, null, "/admin" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10301"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Architecture and solution overview.", 1, "AutoStories", true, false, "solution-overview", null, "Sidebar", "showcase", null, new Guid("0194f700-0000-7000-8000-000000000001"), "Solution Overview", null, null, "/overview" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Reusable platform capability showcases.", 4, "MenuOpen", true, false, "features", null, "Sidebar", "showcase", null, new Guid("0194f700-0000-7000-8000-000000000001"), "Features", null, null, "/features" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10501"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Platform management.", 4, "Dns", true, false, "control-panel", null, "Sidebar", null, "controlplane.manage", new Guid("0194f700-0000-7000-8000-000000000001"), "Control Panel", null, null, "/control-panel" }
                });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10604"),
                columns: new[] { "Key", "PlacementKey" },
                values: new object[] { "admin-customers", "AdminCenter" });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10605"),
                columns: new[] { "Key", "PlacementKey" },
                values: new object[] { "admin-departments", "AdminCenter" });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10606"),
                columns: new[] { "Key", "PlacementKey" },
                values: new object[] { "admin-employees", "AdminCenter" });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10615"),
                columns: new[] { "Key", "Url" },
                values: new object[] { "features-payments", "/features/payments" });

            migrationBuilder.InsertData(
                table: "MenuRoutes",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "PlacementKey", "TenantId", "UpdatedAt", "UpdatedBy", "Url" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10601"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "dashboard", "Dashboard", "Sidebar", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/dashboard" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10602"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-center", "Admin Center", "Sidebar", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10603"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "solution-overview", "Solution Overview", "Sidebar", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/overview" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10607"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-menus", "Menus", "AdminCenter", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/menus" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10608"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-permissions", "Permissions", "AdminCenter", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/permissions" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10609"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-roles", "Roles", "AdminCenter", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/roles" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10610"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-settings", "Settings", "AdminCenter", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/settings" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10611"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-user-devices", "User Devices", "AdminCenter", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/user-devices" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10612"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-users", "Users", "AdminCenter", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/users" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10613"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-access-catalog", "Access Catalog", "AdminCenter", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/access-catalog" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10614"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "features", "Features", "Sidebar", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/features" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10616"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "control-panel", "Control Panel", "Sidebar", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/control-panel" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10617"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "control-panel-tenants", "Tenants", "ControlPanel", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/control-panel/tenants" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10618"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "control-panel-stamps", "Stamps", "ControlPanel", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/control-panel/stamps" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10619"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-org-settings", "Org Settings", "AdminCenter", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/org-settings" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10620"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-iam", "Identity & Access", "AdminCenter", new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/iam" }
                });

            migrationBuilder.UpdateData(
                table: "PermissionContexts",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10105"),
                column: "Description",
                value: "Cross-cutting platform services and reusable capability showcases.");

            migrationBuilder.UpdateData(
                table: "PermissionResources",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10205"),
                column: "ContextKey",
                value: "IAM");

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DepartmentId", "Description", "DisplayOrder", "Icon", "IsActive", "IsDeleted", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "TenantId", "Title", "UpdatedAt", "UpdatedBy", "Url" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20104"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Navigation catalog and menu visibility.", 40, "MenuOpen", true, false, "admin-menus", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "core", null, new Guid("0194f700-0000-7000-8000-000000000001"), "Menus", null, null, "/admin/menus" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20107"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Tenant-specific configuration surface.", 50, "Settings", true, false, "admin-org-settings", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "core", null, new Guid("0194f700-0000-7000-8000-000000000001"), "Org Settings", null, null, "/admin/org-settings" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20110"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Source-of-truth permission and menu reference data.", 70, "LockPerson", true, false, "admin-access-catalog", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "iam", null, new Guid("0194f700-0000-7000-8000-000000000001"), "Access Catalog", null, null, "/admin/access-catalog" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Manage users, roles, permissions, and trusted devices.", 60, "Group", true, false, "admin-iam", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "iam", null, new Guid("0194f700-0000-7000-8000-000000000001"), "Identity & Access", null, null, "/admin/iam" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30101"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Manage SaaS tenants.", 10, "Business", true, false, "control-panel-tenants", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10501"), "ControlPanel", null, "controlplane.manage", new Guid("0194f700-0000-7000-8000-000000000001"), "Tenants", null, null, "/control-panel/tenants" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30102"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Deployment stamps.", 20, "Dns", true, false, "control-panel-stamps", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10501"), "ControlPanel", null, "controlplane.manage", new Guid("0194f700-0000-7000-8000-000000000001"), "Stamps", null, null, "/control-panel/stamps" }
                });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");
        }
    }
}
