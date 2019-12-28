namespace BITCORNService.Utils.Models
{
    public class WithdrawRequest
    {
        public decimal Amount { get; set; }
        public string CornAddy { get; set; }
        public string Id { get; set; }
        public string[] Columns { get; set; }
    }
}
