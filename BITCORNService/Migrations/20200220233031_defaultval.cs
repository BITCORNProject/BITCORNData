using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class defaultval : Migration
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
                nullable: true,
                defaultValueSql: "((0))");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalReferrals",
                table: "UserStat",
                nullable: true,
                defaultValueSql: "((0))");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
