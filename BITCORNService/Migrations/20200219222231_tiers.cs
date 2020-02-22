using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class tiers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferralTier",
                columns: table => new
                {
                    TierId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tier = table.Column<int>(nullable: false),
                    Bonus = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralTier", x => x.TierId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferralTier");
        }
    }
}
