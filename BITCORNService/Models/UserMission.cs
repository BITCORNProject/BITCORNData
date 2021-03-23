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
        public long? Faucet { get; set; }
        public long? NextFaucetTime { get; set; }
        public int? FaucetClaimCount { get; set; }
        public decimal? FaucetFarmAmount { get; set; }
        public bool FaucetAvailable { get; set; }

        public int? FaucetClaimStreak { get; set; }

        public int? FaucetClaimsDuringSub { get; set; }
        public decimal? FaucetStreakBonuses { get; set; }
        public int FaucetRank { get; set; }
        public bool CompletedFlag { get; set; }
        public decimal CompleteReward { get; set; }
        public long UnixTimeNow(DateTime date)
        {
            //var timeSpan = (date- new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)Math.Truncate((date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
            // return (long)timeSpan.TotalSeconds;
        }
        public UserMissionResponse(UserMission m, int faucetRank, bool completedMission, decimal completeReward =0)
        {
            CompletedFlag = completedMission;
            CompleteReward = completeReward;
            DateTime? faucet = m.Faucet;
            if (m != null)
            {
                if (m.Faucet != null)
                {
                    Faucet = UnixTimeNow(m.Faucet.Value);
                    faucet = m.Faucet;
                }

                FaucetClaimCount = m.FaucetClaimCount;
                FaucetFarmAmount = m.FaucetFarmAmount;
                FaucetClaimStreak = m.FaucetClaimStreak;
                FaucetClaimsDuringSub = m.FaucetClaimsDuringSub;
                FaucetStreakBonuses = m.FaucetStreakBonuses;
            }
            FaucetRank = faucetRank+1;
            if (faucet != null)
            {
                FaucetAvailable = DateTime.Now > faucet.Value.AddHours(24);
                if(!FaucetAvailable)
                {
                    NextFaucetTime = UnixTimeNow(faucet.Value.AddHours(24));
                }
            }
            else
            {
                FaucetAvailable = true;
            }
        }
    }
}
