using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class SelectableUser : Dictionary<string,object>
    {
        public int UserId { get; set; }
        public SelectableUser(int userId)
        {
            this.UserId = userId;
        }
    }
}
