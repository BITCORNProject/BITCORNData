using BITCORNService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Games.Models
{
    public partial class BitcornGameContext : BitcornContext
    {
        public BitcornGameContext() : base()
        {
        }

        public BitcornGameContext(DbContextOptions<BitcornContext> options)
            : base(options)
        {

        }
        public virtual DbSet<ItemPrefab> ItemPrefab { get; set; }
        public virtual DbSet<UserAvatar> UserAvatar { get; set; }
        public virtual DbSet<BattlegroundsUserStats> BattlegroundsGameStats { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ItemPrefab>(entity => {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.AddressablePath).HasMaxLength(100);
            });
            modelBuilder.Entity<UserAvatar>(entity=> {
                entity.HasKey(e => e.UserId)
                        .HasName("PK__UserAvatar__1788CC4C85ECFC41");

                entity.Property(e => e.UserId).ValueGeneratedNever();
                entity.Property(e => e.AvatarAddress).HasMaxLength(100);
            });
            modelBuilder.Entity<BattlegroundsUserStats>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("PK__BattlegroundsGameStats__1788CC4C85ECFC41");

                entity.Property(e => e.UserId).ValueGeneratedNever();

                entity.Property(e => e.ConnectedHits)
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.Deaths)
                    .HasDefaultValueSql("((0))");
                entity.Property(e => e.Assists)
                   .HasDefaultValueSql("((0))");
                entity.Property(e => e.DistanceTravelled)
                    .HasDefaultValueSql("((0))");
                entity.Property(e => e.GamesPlayed)
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.HealingDone)
                    .HasDefaultValueSql("((0))");
                
                entity.Property(e => e.Kills)
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.MissedHits)
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.TimeSpentInAir)
                    .HasDefaultValueSql("((0))");
                entity.Property(e => e.TotalAttacks)
                    .HasDefaultValueSql("((0))");
                entity.Property(e => e.TotalCritDamageDealt)
                    .HasDefaultValueSql("((0))");
                entity.Property(e => e.TotalDamageDealt)
                    .HasDefaultValueSql("((0))");
                entity.Property(e => e.TotalDamageTaken)
                    .HasDefaultValueSql("((0))");
                entity.Property(e => e.TotalPickedUpPowerups)
                    .HasDefaultValueSql("((0))");
                entity.Property(e => e.TotalPickedUpWeapons)
                    .HasDefaultValueSql("((0))");
                
            });
        }
    }
}
