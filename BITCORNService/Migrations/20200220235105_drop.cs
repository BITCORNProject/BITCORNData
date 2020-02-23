using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class drop : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalReferralRewards",
                table: "UserStat");

            migrationBuilder.DropColumn(
                name: "TotalReferrals",
                table: "UserStat");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
