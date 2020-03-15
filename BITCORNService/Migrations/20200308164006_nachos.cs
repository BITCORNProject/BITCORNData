using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class nachos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalReferralRewards",
                table: "UserStat");

            migrationBuilder.DropColumn(
                name: "TweetDate",
                table: "UserReferral");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalReferralRewardsCorn",
                table: "UserStat",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalReferralRewardsUsdt",
                table: "UserStat",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.DropColumn(
                name: "SignupReward",
                table: "UserReferral");

            migrationBuilder.AddColumn<DateTime>(
                name: "SignupReward",
                table: "UserReferral",
                nullable: true,
                defaultValue: null);

            migrationBuilder.DropColumn(
                name: "ReferrerBonus",
                table: "UserReferral");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferrerBonus",
                table: "UserReferral",
                nullable: true,
                defaultValue: null);

            migrationBuilder.DropColumn(
                name: "Bonus",
                table: "UserReferral");

            migrationBuilder.AddColumn<DateTime>(
                name: "Bonus",
                table: "UserReferral",
                nullable: true,
                defaultValue: null);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalReferralRewardsCorn",
                table: "UserStat");

            migrationBuilder.DropColumn(
                name: "TotalReferralRewardsUsdt",
                table: "UserStat");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalReferralRewards",
                table: "UserStat",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<bool>(
                name: "SignupReward",
                table: "UserReferral",
                type: "bit",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "ReferrerBonus",
                table: "UserReferral",
                type: "bit",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Bonus",
                table: "UserReferral",
                type: "bit",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TweetDate",
                table: "UserReferral",
                type: "datetime2",
                nullable: true);
        }
    }
}
