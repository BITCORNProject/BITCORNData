using System;

namespace BITCORNService.Reflection
{
    public class UserPropertyRouteAttribute : Attribute
    {
        public string RouteTo { get; set; }
        public UserPropertyRouteAttribute(string routeTo)
        {
            this.RouteTo = routeTo;
        }
    }
}
