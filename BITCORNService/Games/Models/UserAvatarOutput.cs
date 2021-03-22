﻿namespace BITCORNService.Games.Models
{
    public class UserAvatarOutput
	{
		public string Catalog { get; set; }
		public string Avatar { get; set; }
		public string[] AvailableAvatars { get; set; }
		
	}

	public class UserAvatarOutputTwitchName : UserAvatarOutput
    {
		public string Name { get; set; }
    }
}
