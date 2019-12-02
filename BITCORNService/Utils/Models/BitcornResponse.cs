using System.Net;

namespace BITCORNService.Utils.Models
{
    public class BitcornResponse
    {
        public string Message { get; set; }
        public HttpStatusCode HttpCode { get; set; }
    }
}
