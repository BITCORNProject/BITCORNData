namespace BITCORNService.Games.Models
{
	public class BattlegroundsGameStats
	{

		public int UserId { get; set; }
		public int Kills { get; set; }
		public int Deaths { get; set; }
		public int Assists { get; set; }
		public float TotalDamageDealt { get; set; }
		public float TotalDamageTaken { get; set; }
		public float TotalCritDamageDealt { get; set; }
		public float HealingDone { get; set; }
		public float DistanceTravelled { get; set; }
		public int MissedHits { get; set; }
		public int ConnectedHits { get; set; }
		public int TotalAttacks => MissedHits + ConnectedHits;
		public int TotalPickedUpPowerups { get; set; }
		public int TotalPickedUpWeapons { get; set; }
		public float TimeSpentInAir { get; set; }

		public void Add(BattlegroundsGameStats other)
		{
			this.Assists += other.Assists;
			this.ConnectedHits += other.ConnectedHits;
			this.Deaths += other.Deaths;
			this.DistanceTravelled += other.DistanceTravelled;
	
			this.HealingDone += other.HealingDone;
			this.Kills += other.Kills;
			this.MissedHits += other.MissedHits;
			this.TimeSpentInAir += other.TimeSpentInAir;
			this.TotalCritDamageDealt += other.TotalCritDamageDealt;
			this.TotalDamageDealt += other.TotalDamageDealt;
			this.TotalDamageTaken += other.TotalDamageTaken;
			this.TotalPickedUpPowerups += other.TotalPickedUpPowerups;
			this.TotalPickedUpWeapons += other.TotalPickedUpWeapons;
		}
	}
}
