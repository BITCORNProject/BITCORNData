using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BITCORNService.Migrations
{
    public partial class addeddefaultvalues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CornTx",
                columns: table => new
                {
                    CornTxId = table.Column<int>(nullable: false),
                    Platform = table.Column<string>(unicode: false, maxLength: 50, nullable: true),
                    TxType = table.Column<string>(unicode: false, maxLength: 50, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    SenderId = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    ReceiverId = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: true),
                    BlockchainTxId = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CornTx", x => x.CornTxId);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Application = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    Message = table.Column<string>(unicode: false, maxLength: 1000, nullable: true),
                    StackTrace = table.Column<string>(unicode: false, maxLength: 5000, nullable: true),
                    Code = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    TImestamp = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<string>(unicode: false, maxLength: 50, nullable: true),
                    Username = table.Column<string>(unicode: false, maxLength: 50, nullable: true),
                    Avatar = table.Column<string>(unicode: false, maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "WalletIndex",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    Index = table.Column<int>(nullable: true, defaultValueSql: "((1))")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletIndex", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnclaimedTx",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    Expiration = table.Column<DateTime>(type: "datetime", nullable: true),
                    SenderUserId = table.Column<int>(nullable: true),
                    CornTxId = table.Column<int>(nullable: true),
                    Claimed = table.Column<bool>(nullable: true),
                    ReceiverUserId = table.Column<int>(nullable: true),
                    Platform = table.Column<string>(fixedLength: true, maxLength: 10, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    Refunded = table.Column<bool>(nullable: true, defaultValueSql: "((0))"),
                    TxType = table.Column<string>(unicode: false, maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnclaimedTx", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnclaimedTx_CornTx",
                        column: x => x.CornTxId,
                        principalTable: "CornTx",
                        principalColumn: "CornTxId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnclaimedTx_User",
                        column: x => x.ReceiverUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnclaimedTx_SendUser",
                        column: x => x.SenderUserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserIdentity",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    TwitchUsername = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    Auth0Nickname = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    Auth0Id = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    TwitchId = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    DiscordId = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    TwitterId = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    RedditId = table.Column<string>(unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tmp_ms_x__1788CC4C32CEDA3C", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserIdentity_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserStat",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    Tipped = table.Column<int>(nullable: true, defaultValueSql: "((0))"),
                    TIppedTotal = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    TopTiped = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    Tip = table.Column<int>(nullable: true, defaultValueSql: "((0))"),
                    TipTotal = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    TopTip = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    Rained = table.Column<int>(nullable: true, defaultValueSql: "((0))"),
                    RainTotal = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    TopRain = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    RainedOn = table.Column<int>(nullable: true, defaultValueSql: "((0))"),
                    RainedOnTotal = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    TopRainedOn = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    EarnedIdle = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserStat__3214EC0730174CB5", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserStat_Users",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserWallet",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    WalletServer = table.Column<int>(nullable: true),
                    Balance = table.Column<decimal>(type: "numeric(19, 8)", nullable: true, defaultValueSql: "((0))"),
                    CornAddy = table.Column<string>(unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserWall__1788CC4C85ECFC41", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserWallet_User",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnclaimedTx_CornTxId",
                table: "UnclaimedTx",
                column: "CornTxId");

            migrationBuilder.CreateIndex(
                name: "IX_UnclaimedTx_ReceiverUserId",
                table: "UnclaimedTx",
                column: "ReceiverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UnclaimedTx_SenderUserId",
                table: "UnclaimedTx",
                column: "SenderUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "UnclaimedTx");

            migrationBuilder.DropTable(
                name: "UserIdentity");

            migrationBuilder.DropTable(
                name: "UserStat");

            migrationBuilder.DropTable(
                name: "UserWallet");

            migrationBuilder.DropTable(
                name: "WalletIndex");

            migrationBuilder.DropTable(
                name: "CornTx");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
