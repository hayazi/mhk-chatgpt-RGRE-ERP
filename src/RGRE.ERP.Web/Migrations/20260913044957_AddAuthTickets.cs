using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RGRE.ERP.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthTickets",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TicketJson = table.Column<string>(type: "TEXT", maxLength: 16000, nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthTickets", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthTickets");
        }
    }
}
