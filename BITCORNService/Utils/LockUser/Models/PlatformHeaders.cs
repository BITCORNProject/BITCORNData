namespace BITCORNService.Utils.LockUser.Models
{
    public class PlatformHeaders
    {
        public string Platform { get; set; }
        public string Id { get; set; }

        public override string ToString()
        {
            return $"{Platform}:{Id}";
        }
    }
}
