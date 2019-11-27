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
    [Migration("20191127063803_added-default-values")]
    partial class addeddefaultvalues
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.0-preview3.19554.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("BITCORNService.Models.CornTx", b =>
                {
                    b.Property<int>("CornTxId")
                        .HasColumnType("int");

                    b.Property<decimal?>("Amount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<string>("BlockchainTxId")
                        .HasColumnType("nvarchar(100)")
                        .HasMaxLength(100);

                    b.Property<string>("Platform")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.Property<string>("ReceiverId")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<string>("SenderId")
                        .HasColumnType("varchar(100)")
                        .HasMaxLength(100)
                        .IsUnicode(false);

                    b.Property<DateTime?>("Timestamp")
                        .HasColumnType("datetime");

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

                    b.Property<string>("StackTrace")
                        .HasColumnType("varchar(5000)")
                        .HasMaxLength(5000)
                        .IsUnicode(false);

                    b.Property<DateTime?>("Timestamp")
                        .HasColumnName("TImestamp")
                        .HasColumnType("datetime");

                    b.HasKey("Id");

                    b.ToTable("ErrorLogs");
                });

            modelBuilder.Entity("BITCORNService.Models.UnclaimedTx", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<decimal?>("Amount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<bool?>("Claimed")
                        .HasColumnType("bit");

                    b.Property<int?>("CornTxId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("Expiration")
                        .HasColumnType("datetime");

                    b.Property<string>("Platform")
                        .HasColumnType("nchar(10)")
                        .IsFixedLength(true)
                        .HasMaxLength(10);

                    b.Property<int?>("ReceiverUserId")
                        .HasColumnType("int");

                    b.Property<bool?>("Refunded")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValueSql("((0))");

                    b.Property<int?>("SenderUserId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("Timestamp")
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

                    b.Property<string>("Level")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

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

                    b.HasKey("UserId")
                        .HasName("PK__tmp_ms_x__1788CC4C32CEDA3C");

                    b.ToTable("UserIdentity");
                });

            modelBuilder.Entity("BITCORNService.Models.UserStat", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<decimal?>("EarnedIdle")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("RainTotal")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<int?>("Rained")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("((0))");

                    b.Property<int?>("RainedOn")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("RainedOnTotal")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<int?>("Tip")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("TipTotal")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<int?>("Tipped")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("TippedTotal")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("TIppedTotal")
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("TopRain")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("TopRainedOn")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("TopTip")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(19, 8)")
                        .HasDefaultValueSql("((0))");

                    b.Property<decimal?>("TopTiped")
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
                        .HasConstraintName("FK_UnclaimedTx_SendUser");
                });

            modelBuilder.Entity("BITCORNService.Models.UserIdentity", b =>
                {
                    b.HasOne("BITCORNService.Models.User", "User")
                        .WithOne("UserIdentity")
                        .HasForeignKey("BITCORNService.Models.UserIdentity", "UserId")
                        .HasConstraintName("FK_UserIdentity_User")
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
