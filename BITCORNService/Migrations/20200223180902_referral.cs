using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class referral : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Referrer");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Referrer");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Referrer");

            migrationBuilder.DropColumn(
                name: "W9Url",
                table: "Referrer");

            migrationBuilder.AddColumn<string>(
                name: "ETag",
                table: "Referrer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Referrer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ETag",
                table: "Referrer");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Referrer");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Referrer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Referrer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Referrer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "W9Url",
                table: "Referrer",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
