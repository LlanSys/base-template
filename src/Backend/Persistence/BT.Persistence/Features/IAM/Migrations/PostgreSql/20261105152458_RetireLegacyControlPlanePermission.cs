using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BT.Persistence.Features.IAM.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class RetireLegacyControlPlanePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "RoleClaims"
                WHERE "ClaimType" = 'permission' AND LOWER("ClaimValue") = 'controlplane.manage';

                DELETE FROM "UserClaims"
                WHERE "ClaimType" = 'permission' AND LOWER("ClaimValue") = 'controlplane.manage';

                DELETE FROM "Permissions"
                WHERE LOWER("Key") = 'controlplane.manage';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the catalog seed. The pre-upgrade backup is the source for old assignments.
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "AvailableTenantId", "Context", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DepartmentId", "Description", "IsActive", "IsDeleted", "IsTenantDelegatable", "Key", "RequiredModule", "Resource", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10801"), "manage", null, "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, null, "Manage Control Plane Tenants and Stamps.", true, false, false, "controlplane.manage", null, "controlplane", new Guid("0194f700-0000-7000-8000-000000000001"), null, null });
        }
    }
}
