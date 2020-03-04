using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class referralstats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalReferralRewards",
                table: "UserStat",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalReferrals",
                table: "UserStat",
                nullable: true);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalReferralRewards",
                table: "UserStat");

            migrationBuilder.DropColumn(
                name: "TotalReferrals",
                table: "UserStat");
        }
    }
}
