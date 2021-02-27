using System;
using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class UserMission
    {
        [Key]
        public int UserId { get; set; }
        public DateTime? Faucet { get; set; }

        public int? FaucetClaimCount { get; set; }
        public decimal? FaucetFarmAmount { get; set; }
        public decimal? FaucetStreakBonuses { get; set; }
        public int? FaucetClaimStreak { get; set; }

        public int? FaucetClaimsDuringSub { get; set; }
        public int? FaucetSubIndex { get; set; }
    }

    public class UserMissionResponse
    {
        public DateTime? Faucet { get; set; }

        public int? FaucetClaimCount { get; set; }
        public decimal? FaucetFarmAmount { get; set; }
        public bool FaucetAvailable { get; set; }

        public int? FaucetClaimStreak { get; set; }

        public int? FaucetClaimsDuringSub { get; set; }
        public decimal? FaucetStreakBonuses { get; set; }
        public int FaucetRank { get; set; }
        public UserMissionResponse(UserMission m, int faucetRank)
        {
            if (m != null)
            {
                Faucet = m.Faucet;
                FaucetClaimCount = m.FaucetClaimCount;
                FaucetFarmAmount = m.FaucetFarmAmount;
                FaucetClaimStreak = m.FaucetClaimStreak;
                FaucetClaimsDuringSub = m.FaucetClaimsDuringSub;
                FaucetStreakBonuses = m.FaucetStreakBonuses;
            }
            FaucetRank = faucetRank+1;
            if (Faucet != null)
            {
                FaucetAvailable = DateTime.Now > Faucet.Value.AddHours(24);
            }
            else
            {
                FaucetAvailable = true;
            }
        }
    }
}
