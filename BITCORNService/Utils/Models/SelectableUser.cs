using BITCORNService.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BITCORNService.Utils.Models
{
    public class SelectableUser : Dictionary<string,object>
    {
        [JsonIgnore]
        public User User { get; set; }
        public int UserId
        {
            get
            {
                if (User != null)
                {
                    return User.UserId;
                }
                else
                {
                    return -1;
                }
            }
        }
        public SelectableUser(User user)
        {
            this.User = user;
        }
    }
}
