using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Models
{

    public class TwitchSubData
    {
        public int _total { get; set; }
        public TwitchSubscription[] subscriptions { get; set; }
    }

    public class TwitchSubscription
    {
        public DateTime created_at { get; set; }
        public string _id { get; set; }
        public string sub_plan { get; set; }
        public string sub_plan_name { get; set; }
        public bool is_gift { get; set; }
        public User2 user { get; set; }
        public object sender { get; set; }
    }

    public class User2
    {
        public string display_name { get; set; }
        public string type { get; set; }
        public string bio { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string name { get; set; }
        public string _id { get; set; }
        public string logo { get; set; }
    }
}
