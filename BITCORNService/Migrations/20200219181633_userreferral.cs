using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class userreferral : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotalReferralRewards",
                table: "UserStat",
                type: "numeric(19, 8)",
                nullable: true,
                defaultValueSql: "((0))",
                oldClrType: typeof(decimal),
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalReferrals",
                table: "UserStat",
                type: "int",
                nullable: true,
                defaultValueSql: "((0))",
                oldClrType: typeof(decimal),
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "UserReferral",
                columns: table => new
                {
                    UserReferralId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    ReferralId = table.Column<int>(nullable: false),
                    WalletDownloadDate = table.Column<DateTime>(nullable: true),
                    MinimumBalanceDate = table.Column<DateTime>(nullable: true),
                    Bonus = table.Column<bool>(nullable: false, defaultValue: false),
                    ReferrerBonus = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReferral", x => x.UserReferralId);
                    table.ForeignKey(
                        name: "FK_UserReferral_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Referrer_UserId",
                table: "Referrer",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReferral_UserId",
                table: "UserReferral",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Referrer_User_UserId",
                table: "Referrer",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Referrer_User_UserId",
                table: "Referrer");

            migrationBuilder.DropTable(
                name: "UserReferral");

        }
    }
}
