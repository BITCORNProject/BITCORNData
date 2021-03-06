﻿using System;
using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Games.Models
{
    public class GameInstance
	{
		[Key]
		public int GameId { get; set; }
		public int HostId { get; set; }
		public decimal Payin { get; set; }
		public decimal Reward { get; set; }
		public int RewardMultiplier { get; set; }
		public int PlayerLimit { get; set; }
		public bool Active { get; set; }
		public bool Started { get; set; }
		public int? HostDebitCornTxId { get; set; }
		public string TournamentId { get; set; }
		public int GameMode { get; set; }
		public bool EnableTeams { get; set; }
		public int? LastTeamSeed { get; set; }
		public bool Bgrains { get; set; }
		public string MapId { get; set; }
		public string Data { get; set; }
		public BattlegroundsGameMode GetGameMode() => (BattlegroundsGameMode)GameMode;
		public void CopySettings(GameInstance previousGame)
        {
			Payin = previousGame.Payin;
			PlayerLimit = previousGame.PlayerLimit;
			GameMode = previousGame.GameMode;
			EnableTeams = previousGame.EnableTeams;
			LastTeamSeed = previousGame.LastTeamSeed;
			Bgrains = previousGame.Bgrains;
			Data = previousGame.Data;
		}
    }
	public enum BattlegroundsGameMode
	{
		Pvp, Olympics, Raidboss
	}
	public class Tournament
	{

		[Key]
		public string TournamentId { get; set; }
		public int UserId { get; set; }
		public int MapCount { get; set; }
		public int MapIndex { get; set; }
		public bool Completed { get; set; }
		public int PointMethod { get; set; }
		public int? PreviousMapId { get; set; }
		public DateTime? StartTime { get; set; }
		public string TournamentData { get; set; }
		public bool JoiningBetweenTournamentGames { get; set; }
	}
}
