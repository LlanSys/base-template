using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BT.Persistence.Features.ControlPlane.Migrations.PostgreSql
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
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedAt",
                table: "TenantAdministratorInvitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimedUserId",
                table: "TenantAdministratorInvitations",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "TenantAdministratorInvitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "TenantAdministratorInvitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "TenantAdministratorInvitations",
                type: "character varying(64)",
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
