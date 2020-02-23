﻿// <auto-generated />
using System;
using BITCORNService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BITCORNService.Migrations
{
    [DbContext(typeof(BitcornContext))]
    [Migration("20200221223652_drop6")]
    partial class drop6
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("BITCORNService.Models.CornDeposit", b =>
                {
                    b.Property<string>("TxId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("UserId")
                        .HasColumnName("UserId")
                        .HasColumnType("int");

                    b.HasKey("TxId");

                    b.ToTable("CornDeposit");
                });

            modelBuilder.Entity("BITCORNService.Models.CornTx", b =>
                {
                    b.Property<int>("CornTxId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal?>("Amount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<string>("BlockchainTxId")
                        .HasColumnType("nvarchar(100)")
                        .HasMaxLength(100);

                    b.Property<string>("CornAddy")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("Platform")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<int?>("ReceiverId")
                        .HasColumnType("int");

                    b.Property<int?>("SenderId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("Timestamp")
                        .HasColumnType("datetime");

                    b.Property<string>("TxGroupId")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("TxType")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.HasKey("CornTxId");

                    b.ToTable("CornTx");
                });

            modelBuilder.Entity("BITCORNService.Models.ErrorLogs", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Application")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("Code")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("Message")
                        .HasColumnType("varchar(1000)")
                        .HasMaxLength(1000)
                        .IsUnicode(false);

                    b.Property<string>("RequestBody")
                        .HasColumnType("nvarchar(1000)")
                        .HasMaxLength(1000);

                    b.Property<string>("StackTrace")
                        .HasColumnType("varchar(5000)")
                        .HasMaxLength(5000)
                        .IsUnicode(false);

                    b.Property<DateTime?>("Timestamp")
                        .HasColumnType("datetime");

                    b.HasKey("Id");

                    b.ToTable("ErrorLogs");
                });

            modelBuilder.Entity("BITCORNService.Models.ReferralTier", b =>
                {
                    b.Property<int>("TierId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("Bonus")
                        .HasColumnType("int");

                    b.Property<int>("Tier")
                        .HasColumnType("int");

                    b.HasKey("TierId");

                    b.ToTable("ReferralTier");
                });

            modelBuilder.Entity("BITCORNService.Models.Referrer", b =>
                {
                    b.Property<int>("ReferralId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Tier")
                        .HasColumnType("int");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("ReferralId");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Referrer");
                });

            modelBuilder.Entity("BITCORNService.Models.UnclaimedTx", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("Amount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<bool>("Claimed")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValueSql("((0))");

                    b.Property<int?>("CornTxId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Expiration")
                        .HasColumnType("datetime");

                    b.Property<string>("Platform")
                        .HasColumnType("nchar(10)")
                        .IsFixedLength(true)
                        .HasMaxLength(10);

                    b.Property<string>("ReceiverPlatformId")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<int?>("ReceiverUserId")
                        .HasColumnType("int");

                    b.Property<bool>("Refunded")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValueSql("((0))");

                    b.Property<int>("SenderUserId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime");

                    b.Property<string>("TxType")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.HasKey("Id");

                    b.HasIndex("CornTxId");

                    b.HasIndex("ReceiverUserId");

                    b.HasIndex("SenderUserId");

                    b.ToTable("UnclaimedTx");
                });

            modelBuilder.Entity("BITCORNService.Models.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Avatar")
                        .HasColumnType("varchar(2048)")
                        .HasMaxLength(2048)
                        .IsUnicode(false);

                    b.Property<bool>("IsBanned")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("IsBanned")
                        .HasColumnType("bit")
                        .HasDefaultValueSql("((0))");

                    b.Property<string>("Level")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<int>("SubTier")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("((0))");

                    b.Property<string>("Username")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.HasKey("UserId");

                    b.ToTable("User");
                });

            modelBuilder.Entity("BITCORNService.Models.UserIdentity", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<string>("Auth0Id")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("Auth0Nickname")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("DiscordId")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("DiscordUsername")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("RedditId")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("TwitchId")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("TwitchUsername")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("TwitterId")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("TwitterUsername")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.HasKey("UserId")
                        .HasName("PK__tmp_ms_x__1788CC4C32CEDA3C");

                    b.ToTable("UserIdentity");
                });

            modelBuilder.Entity("BITCORNService.Models.UserReferral", b =>
                {
                    b.Property<int>("UserReferralId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("Bonus")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("MinimumBalanceDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("ReferralId")
                        .HasColumnType("int");

                    b.Property<bool>("ReferrerBonus")
                        .HasColumnType("bit");

                    b.Property<bool>("SignupReward")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("SyncDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("TweetDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("WalletDownloadDate")
                        .HasColumnType("datetime2");

                    b.HasKey("UserReferralId");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("UserReferral");
                });

            modelBuilder.Entity("BITCORNService.Models.UserStat", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<int?>("AmountOfRainsReceived")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("((0))");

                    b.Property<int?>("AmountOfRainsSent")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("((0))");

                    b.Property<int?>("AmountOfTipsReceived")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("((0))");

                    b.Property<int?>("AmountOfTipsSent")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("EarnedIdle")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("LargestReceivedBitcornRain")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("LargestReceivedBitcornTip")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("LargestSentBitcornRain")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("LargestSentBitcornTip")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("TotalReceivedBitcornRains")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("TotalReceivedBitcornTips")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal>("TotalReferralRewards")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("TotalReferrals")
                        .HasColumnType("int");

                    b.Property<decimal?>("TotalSentBitcornViaRains")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("TotalSentBitcornViaTips")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.HasKey("UserId")
                        .HasName("PK__UserStat__3214EC0730174CB5");

                    b.ToTable("UserStat");
                });

            modelBuilder.Entity("BITCORNService.Models.UserWallet", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<decimal?>("Balance")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<string>("CornAddy")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<int?>("WalletServer")
                        .HasColumnType("int");

                    b.HasKey("UserId")
                        .HasName("PK__UserWall__1788CC4C85ECFC41");

                    b.ToTable("UserWallet");
                });

            modelBuilder.Entity("BITCORNService.Models.WalletDownload", b =>
                {
                    b.Property<int>("DownloadId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Country")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IPAddress")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IncomingUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Platform")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ReferralCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("ReferralId")
                        .HasColumnType("int");

                    b.Property<int?>("ReferralUserId")
                        .HasColumnType("int");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("datetime2");

                    b.Property<int?>("UserId")
                        .HasColumnType("int");

                    b.Property<string>("WalletVersion")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("DownloadId");

                    b.ToTable("WalletDownload");
                });

            modelBuilder.Entity("BITCORNService.Models.WalletIndex", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<int?>("Index")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("((1))");

                    b.HasKey("Id");

                    b.ToTable("WalletIndex");
                });

            modelBuilder.Entity("BITCORNService.Models.WalletServer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("Id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("DepositAddress")
                        .HasColumnName("DepositAddress")
                        .HasColumnType("nvarchar(50)")
                        .HasMaxLength(50);

                    b.Property<bool>("Enabled")
                        .HasColumnName("Enabled")
                        .HasColumnType("bit");

                    b.Property<string>("Endpoint")
                        .IsRequired()
                        .HasColumnName("Endpoint")
                        .HasColumnType("nvarchar(50)")
                        .HasMaxLength(50);

                    b.Property<int>("Index")
                        .HasColumnName("index")
                        .HasColumnType("int");

                    b.Property<string>("LastBalanceUpdateBlock")
                        .HasColumnName("LastBalanceUpdateBlock")
                        .HasColumnType("nvarchar(100)")
                        .HasMaxLength(100);

                    b.Property<decimal?>("ServerBalance")
                        .HasColumnName("ServerBalance")
                        .HasColumnType("numeric(19, 8)");

                    b.Property<bool>("WithdrawEnabled")
                        .HasColumnName("WithdrawEnabled")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("WalletServer");
                });

            modelBuilder.Entity("BITCORNService.Models.Referrer", b =>
                {
                    b.HasOne("BITCORNService.Models.User", "User")
                        .WithOne("Referral")
                        .HasForeignKey("BITCORNService.Models.Referrer", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BITCORNService.Models.UnclaimedTx", b =>
                {
                    b.HasOne("BITCORNService.Models.CornTx", "CornTx")
                        .WithMany("UnclaimedTx")
                        .HasForeignKey("CornTxId")
                        .HasConstraintName("FK_UnclaimedTx_CornTx");

                    b.HasOne("BITCORNService.Models.User", "ReceiverUser")
                        .WithMany("UnclaimedTxReceiverUser")
                        .HasForeignKey("ReceiverUserId")
                        .HasConstraintName("FK_UnclaimedTx_User");

                    b.HasOne("BITCORNService.Models.User", "SenderUser")
                        .WithMany("UnclaimedTxSenderUser")
                        .HasForeignKey("SenderUserId")
                        .HasConstraintName("FK_UnclaimedTx_SendUser")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BITCORNService.Models.UserIdentity", b =>
                {
                    b.HasOne("BITCORNService.Models.User", "User")
                        .WithOne("UserIdentity")
                        .HasForeignKey("BITCORNService.Models.UserIdentity", "UserId")
                        .HasConstraintName("FK_UserIdentity_User")
                        .IsRequired();
                });

            modelBuilder.Entity("BITCORNService.Models.UserReferral", b =>
                {
                    b.HasOne("BITCORNService.Models.User", "User")
                        .WithOne("UserReferral")
                        .HasForeignKey("BITCORNService.Models.UserReferral", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BITCORNService.Models.UserStat", b =>
                {
                    b.HasOne("BITCORNService.Models.User", "User")
                        .WithOne("UserStat")
                        .HasForeignKey("BITCORNService.Models.UserStat", "UserId")
                        .HasConstraintName("FK_UserStat_Users")
                        .IsRequired();
                });

            modelBuilder.Entity("BITCORNService.Models.UserWallet", b =>
                {
                    b.HasOne("BITCORNService.Models.User", "User")
                        .WithOne("UserWallet")
                        .HasForeignKey("BITCORNService.Models.UserWallet", "UserId")
                        .HasConstraintName("FK_UserWallet_User")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
