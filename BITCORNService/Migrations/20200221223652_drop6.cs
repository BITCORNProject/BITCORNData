using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class drop6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SyncDate",
                table: "UserReferral",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TweetDate",
                table: "UserReferral",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SyncDate",
                table: "UserReferral");

            migrationBuilder.DropColumn(
                name: "TweetDate",
                table: "UserReferral");
        }
    }
}
