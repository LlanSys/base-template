using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BT.Persistence.Features.ControlPlane.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AdministratorInvitationAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcceptanceStatus",
                table: "TenantAdministratorInvitations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedAt",
                table: "TenantAdministratorInvitations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimedUserId",
                table: "TenantAdministratorInvitations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "TenantAdministratorInvitations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "TenantAdministratorInvitations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "TenantAdministratorInvitations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptanceStatus",
                table: "TenantAdministratorInvitations");

            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "TenantAdministratorInvitations");

            migrationBuilder.DropColumn(
                name: "ClaimedUserId",
                table: "TenantAdministratorInvitations");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "TenantAdministratorInvitations");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "TenantAdministratorInvitations");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "TenantAdministratorInvitations");
        }
    }
}
