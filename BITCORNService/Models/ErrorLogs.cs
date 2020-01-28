using System;

namespace BITCORNService.Models
{
    public class ErrorLogs
    {
        public int Id { get; set; }
        public string Application { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Code { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
