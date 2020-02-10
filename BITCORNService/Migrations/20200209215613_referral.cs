using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class referral : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacebookId",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookName",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramId",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramName",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInId",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInName",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinterestId",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinterestName",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapchatId",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapchatName",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TikTokId",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TikTokName",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YouTubeId",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YouTubeName",
                table: "UserIdentity",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SubTier",
                table: "User",
                nullable: false,
                defaultValueSql: "((0))",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "Referrer",
                columns: table => new
                {
                    ReferralId = table.Column<string>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Amount = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrer", x => x.ReferralId);
                    table.ForeignKey(
                        name: "FK_Referrer_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WalletDownload",
                columns: table => new
                {
                    DownloadId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferralId = table.Column<int>(nullable: false),
                    ReferralUserId = table.Column<int>(nullable: true),
                    ReferralCode = table.Column<string>(nullable: true),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    IncomingUrl = table.Column<string>(nullable: true),
                    UserId = table.Column<int>(nullable: true),
                    Country = table.Column<string>(nullable: true),
                    IPAddress = table.Column<string>(nullable: true),
                    Platform = table.Column<string>(nullable: true),
                    WalletVersion = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletDownload", x => x.DownloadId);
                    table.ForeignKey(
                        name: "FK_WalletDownload_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletDownload_User2",
                        column: x => x.ReferralUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Referrer");

            migrationBuilder.DropTable(
                name: "WalletDownload");

            migrationBuilder.DropColumn(
                name: "FacebookId",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "FacebookName",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "InstagramId",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "InstagramName",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "LinkedInId",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "LinkedInName",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "PinterestId",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "PinterestName",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "SnapchatId",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "SnapchatName",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "TikTokId",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "TikTokName",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "YouTubeId",
                table: "UserIdentity");

            migrationBuilder.DropColumn(
                name: "YouTubeName",
                table: "UserIdentity");

            migrationBuilder.AlterColumn<int>(
                name: "SubTier",
                table: "User",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldDefaultValueSql: "((0))");
        }
    }
}
