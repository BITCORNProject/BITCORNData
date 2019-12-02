using System.Net;

namespace BITCORNService.Utils.Models
{
    public class BitcornResponse
    {
        public string Message { get; set; }
        public int BitcornCode { get; set; }
        public string Type { get; set; }
        private HttpStatusCode HttpCode { get; set; }
    }
}
