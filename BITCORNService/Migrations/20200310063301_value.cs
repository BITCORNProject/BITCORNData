using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class value : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalUsdtValue",
                table: "CornTx",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UsdtPrice",
                table: "CornTx",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalUsdtValue",
                table: "CornTx");

            migrationBuilder.DropColumn(
                name: "UsdtPrice",
                table: "CornTx");
        }
    }
}
