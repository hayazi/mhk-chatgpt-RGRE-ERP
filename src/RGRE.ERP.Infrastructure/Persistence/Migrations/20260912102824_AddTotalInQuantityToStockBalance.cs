using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RGRE.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalInQuantityToStockBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalInQuantity",
                table: "StockBalances",
                type: "TEXT",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalInQuantity",
                table: "StockBalances");
        }
    }
}
