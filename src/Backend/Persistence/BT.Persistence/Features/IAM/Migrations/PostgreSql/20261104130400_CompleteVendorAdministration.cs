using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BT.Persistence.Features.IAM.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class CompleteVendorAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PermissionActions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10312"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "view_tenants", "View tenants", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10313"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "edit_tenants", "Edit tenants", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10314"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "delete_tenants", "Delete tenants", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10315"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "manage_tenant_modules", "Manage tenant modules", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10316"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "manage_tenant_administrators", "Manage tenant administrators", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10317"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "manage_tenant_lifecycle", "Manage tenant lifecycle", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10318"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "migrate_tenants", "Migrate tenants", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10319"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "view_stamps", "View stamps", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10320"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "manage_stamps", "Manage stamps", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10321"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "view_audit", "View audit", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10322"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Vendor operation.", true, false, "impersonate", "Impersonate", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "AvailableTenantId", "Context", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DepartmentId", "Description", "IsActive", "IsDeleted", "IsTenantDelegatable", "Key", "RequiredModule", "Resource", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11001"), "view_tenants", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "View tenants.", true, false, false, "controlplane.view_tenants", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11002"), "edit_tenants", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Edit tenants.", true, false, false, "controlplane.edit_tenants", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11003"), "delete_tenants", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Delete tenants.", true, false, false, "controlplane.delete_tenants", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11004"), "manage_tenant_modules", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Manage tenant modules.", true, false, false, "controlplane.manage_tenant_modules", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11005"), "manage_tenant_administrators", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Manage tenant administrators.", true, false, false, "controlplane.manage_tenant_administrators", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11006"), "manage_tenant_lifecycle", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Manage tenant lifecycle.", true, false, false, "controlplane.manage_tenant_lifecycle", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11007"), "migrate_tenants", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Migrate tenants.", true, false, false, "controlplane.migrate_tenants", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11008"), "view_stamps", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "View stamps.", true, false, false, "controlplane.view_stamps", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11009"), "manage_stamps", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Manage stamps.", true, false, false, "controlplane.manage_stamps", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11010"), "view_audit", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "View audit.", true, false, false, "controlplane.view_audit", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11011"), "impersonate", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Impersonate.", true, false, false, "controlplane.impersonate", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11012"), "view", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "View module definitions and readiness.", true, false, false, "modules.view", null, "modules", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10312"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10313"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10314"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10315"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10316"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10317"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10318"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10319"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10320"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10321"));

            migrationBuilder.DeleteData(
                table: "PermissionActions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10322"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11001"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11002"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11003"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11004"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11005"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11006"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11007"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11008"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11009"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11010"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11011"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b11012"));
        }
    }
}
