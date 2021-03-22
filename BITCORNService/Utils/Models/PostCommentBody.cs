namespace BITCORNService.Utils.Models
{
    public class PostCommentBody
    {
        public string Message { get; set; }
        public string ParentId { get; set; }
        public string ContextId { get; set; }
        public string Context { get; set; }
        public string MediaId { get; set; }
    }
}
