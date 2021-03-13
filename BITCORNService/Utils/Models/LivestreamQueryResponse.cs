using BITCORNService.Models;

namespace BITCORNService.Utils.Models
{
    public class LivestreamQueryResponse
    {
        //public int UserId => User.UserId;//{ get; set; }
        //public UserIdentity UserIdentity { get; set; }
        public User User { get; set; }
        public UserLivestream Stream { get; set; }
    }
}
