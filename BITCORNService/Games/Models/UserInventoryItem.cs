﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Games.Models
{
    public class UserInventoryItem
    {
        public int ItemInstanceId { get; set; }
        public int UserId { get; set; }
        public int ItemPrefabId { get; set; }
        public string Type { get; set; }
    }
}
