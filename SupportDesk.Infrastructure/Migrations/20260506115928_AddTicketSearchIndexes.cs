using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_tickets_assigned_agent_id",
                table: "tickets",
                column: "assigned_agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_created_at",
                table: "tickets",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_created_by_user_id",
                table: "tickets",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_priority",
                table: "tickets",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_status",
                table: "tickets",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tickets_assigned_agent_id",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_created_at",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_created_by_user_id",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_priority",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_status",
                table: "tickets");
        }
    }
}
