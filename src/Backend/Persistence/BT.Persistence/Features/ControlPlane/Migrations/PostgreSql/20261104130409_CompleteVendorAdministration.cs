using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BT.Persistence.Features.ControlPlane.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class CompleteVendorAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantAdministratorInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastFailure = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAdministratorInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantAdministratorInvitations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "DeploymentStamps",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000001"),
                column: "RowVersion",
                value: Array.Empty<byte>());

            migrationBuilder.UpdateData(
                table: "TenantModules",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000002"),
                column: "RowVersion",
                value: Array.Empty<byte>());

            migrationBuilder.UpdateData(
                table: "TenantModules",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000003"),
                column: "RowVersion",
                value: Array.Empty<byte>());

            migrationBuilder.UpdateData(
                table: "TenantModules",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000004"),
                column: "RowVersion",
                value: Array.Empty<byte>());

            migrationBuilder.UpdateData(
                table: "TenantModules",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000005"),
                column: "RowVersion",
                value: Array.Empty<byte>());

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: new Guid("0194f700-0000-7000-8000-000000000001"),
                column: "RowVersion",
                value: Array.Empty<byte>());

            migrationBuilder.CreateIndex(
                name: "IX_TenantAdministratorInvitations_Status_NextAttemptAt",
                table: "TenantAdministratorInvitations",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAdministratorInvitations_TenantId_NormalizedEmail",
                table: "TenantAdministratorInvitations",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantAdministratorInvitations");
        }
    }
}
