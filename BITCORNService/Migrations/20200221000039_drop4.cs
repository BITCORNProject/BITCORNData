using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class drop4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalReferralRewards",
                table: "UserStat");

            migrationBuilder.DropColumn(
                name: "TotalReferrals",
                table: "UserStat");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalReferralRewards",
                table: "UserStat",
                nullable: false,
                defaultValue: 0,
                defaultValueSql: "((0))");

            migrationBuilder.AddColumn<int>(
                name: "TotalReferrals",
                table: "UserStat",
                nullable: false,
                defaultValue: 0,
                defaultValueSql: "((0))");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
