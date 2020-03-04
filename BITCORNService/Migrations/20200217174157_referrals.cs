using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class referrals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferralReward",
                table: "UserStat");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
