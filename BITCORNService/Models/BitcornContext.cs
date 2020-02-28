using Microsoft.EntityFrameworkCore;
 
namespace BITCORNService.Models
{
    public partial class BitcornContext : DbContext
    {
        public BitcornContext()
        {
        }

        public BitcornContext(DbContextOptions<BitcornContext> options)
            : base(options)
        {
        }

        public virtual DbSet<CornTx> CornTx { get; set; }
        public virtual DbSet<ErrorLogs> ErrorLogs { get; set; }
        public virtual DbSet<UnclaimedTx> UnclaimedTx { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<UserIdentity> UserIdentity { get; set; }
        public virtual DbSet<UserStat> UserStat { get; set; }
        public virtual DbSet<UserWallet> UserWallet { get; set; }
        public virtual DbSet<WalletIndex> WalletIndex { get; set; }
        public virtual DbSet<WalletServer> WalletServer { get; set; }
        public virtual DbSet<CornDeposit> CornDeposit { get; set; }
        public virtual DbSet<WalletDownload> WalletDownload { get; set; }
        public virtual DbSet<Referrer> Referrer { get; set; }
        public virtual DbSet<UserReferral> UserReferral { get; set; }
        public virtual DbSet<ReferralTier> ReferralTier { get; set; }
        public virtual DbSet<UserSubscription> UserSubscription { get; set; }
        

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CornTx>(entity =>
            {
                entity.Property(e => e.CornTxId);

                entity.Property(e => e.Amount)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.BlockchainTxId).HasMaxLength(100);

                entity.Property(e => e.Platform)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ReceiverId);

                entity.Property(e => e.SenderId);

                entity.Property(e => e.Timestamp).HasColumnType("datetime");

                entity.Property(e => e.TxType)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e=>e.TxGroupId)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.CornAddy)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<ErrorLogs>(entity =>
            {
                entity.Property(e => e.Application)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Code)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Message)
                    .HasMaxLength(1000)
                    .IsUnicode(false);

                entity.Property(e => e.StackTrace)
                    .HasMaxLength(5000)
                    .IsUnicode(false);

                entity.Property(e => e.Timestamp)
                    .HasColumnType("datetime");

                entity.Property(e=>e.RequestBody)
                    .HasMaxLength(1000);
            });

            modelBuilder.Entity<UnclaimedTx>(entity =>
            {
                entity.Property(e => e.Id);

                entity.Property(e => e.Amount)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.Expiration).HasColumnType("datetime");

                entity.Property(e => e.Platform)
                    .HasMaxLength(10)
                    .IsFixedLength();

                entity.Property(e => e.Refunded).HasDefaultValueSql("((0))");
                entity.Property(e => e.Claimed).HasDefaultValueSql("((0))"); ;
                entity.Property(e => e.Timestamp).HasColumnType("datetime");

                entity.Property(e => e.TxType)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.CornTx)
                    .WithMany(p => p.UnclaimedTx)
                    .HasForeignKey(d => d.CornTxId)
                    .HasConstraintName("FK_UnclaimedTx_CornTx");

                entity.HasOne(d => d.ReceiverUser)
                    .WithMany(p => p.UnclaimedTxReceiverUser)
                    .HasForeignKey(d => d.ReceiverUserId)
                    .HasConstraintName("FK_UnclaimedTx_User");

                entity.HasOne(d => d.SenderUser)
                    .WithMany(p => p.UnclaimedTxSenderUser)
                    .HasForeignKey(d => d.SenderUserId)
                    .HasConstraintName("FK_UnclaimedTx_SendUser");

                entity.Property(e=>e.ReceiverPlatformId)
                    .HasMaxLength(50)
                    .IsUnicode(false);

               
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Avatar)
                    .HasMaxLength(2048)
                    .IsUnicode(false);

                entity.Property(e => e.Level)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Username)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e=>e.IsBanned)
                    .HasColumnName("IsBanned")
                    .HasDefaultValueSql("((0))");

                entity.Property(e=>e.SubTier)
                    .HasDefaultValueSql("((0))");
            });

            modelBuilder.Entity<UserIdentity>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("PK__tmp_ms_x__1788CC4C32CEDA3C");

                entity.Property(e => e.UserId).ValueGeneratedNever();

                entity.Property(e => e.Auth0Id)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Auth0Nickname)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e=>e.TwitterUsername)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e=>e.DiscordUsername)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DiscordId)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.RedditId)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.TwitchId)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.TwitchUsername)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.TwitterId)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithOne(p => p.UserIdentity)
                    .HasForeignKey<UserIdentity>(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserIdentity_User");
            });

            modelBuilder.Entity<UserStat>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("PK__UserStat__3214EC0730174CB5");

                entity.Property(e => e.UserId).ValueGeneratedNever();

                entity.Property(e => e.EarnedIdle)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalSentBitcornViaRains)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.AmountOfRainsSent).HasDefaultValueSql("((0))");

                entity.Property(e => e.AmountOfRainsReceived).HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalReceivedBitcornRains)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.AmountOfTipsSent).HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalSentBitcornViaTips)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.AmountOfTipsReceived).HasDefaultValueSql("((0))");

                entity.Property(e => e.TotalReceivedBitcornTips)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.LargestSentBitcornRain)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.LargestReceivedBitcornRain)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.LargestSentBitcornTip)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.LargestReceivedBitcornTip)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.HasOne(d => d.User)
                    .WithOne(p => p.UserStat)
                    .HasForeignKey<UserStat>(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserStat_Users");
            });

            modelBuilder.Entity<UserWallet>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("PK__UserWall__1788CC4C85ECFC41");

                entity.Property(e => e.UserId).ValueGeneratedNever();

                entity.Property(e => e.Balance)
                    .HasColumnType("numeric(19, 8)")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.CornAddy)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.User)
                    .WithOne(p => p.UserWallet)
                    .HasForeignKey<UserWallet>(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserWallet_User");
            });

            modelBuilder.Entity<WalletIndex>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Index).HasDefaultValueSql("((1))");
            });
            modelBuilder.Entity<CornDeposit>(entity => {
                entity.ToTable("CornDeposit");

                entity.Property(e => e.TxId);

                entity.Property(e=>e.UserId).HasColumnName("UserId");
            });
            modelBuilder.Entity<WalletServer>(entity =>
            {
                entity.ToTable("WalletServer");

                entity.Property(e => e.Id).HasColumnName("Id");

                entity.Property(e => e.DepositAddress)
                    .HasColumnName("DepositAddress")
                    .HasMaxLength(50);

                entity.Property(e => e.Endpoint)
                    .IsRequired()
                    .HasColumnName("Endpoint")
                    .HasMaxLength(50);

                entity.Property(e => e.Index).HasColumnName("index");

                entity.Property(e => e.LastBalanceUpdateBlock)
                    .HasColumnName("LastBalanceUpdateBlock")
                    .HasMaxLength(100);

                entity.Property(e => e.ServerBalance)
                    .HasColumnName("ServerBalance")
                    .HasColumnType("numeric(19, 8)");

                entity.Property(e => e.Enabled).HasColumnName("Enabled");
                entity.Property(e => e.WithdrawEnabled).HasColumnName("WithdrawEnabled");
            });
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
