using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BT.Persistence.Features.Shared.Migrations.SqlServer
{
    public partial class PinMassTransit8Outbox : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                table: "OutboxMessage",
                column: "ExpirationTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                table: "OutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                table: "OutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_OutboxState_Created",
                table: "OutboxState");
        }
    }
}
