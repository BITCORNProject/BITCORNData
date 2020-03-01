using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class ytd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "YtdTotal",
                table: "Referrer",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YtdTotal",
                table: "Referrer");
        }
    }
}
