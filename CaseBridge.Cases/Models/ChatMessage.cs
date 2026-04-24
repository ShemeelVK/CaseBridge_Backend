namespace CaseBridge_Cases.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;  
        public string RoomType { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime SendAt { get; set; } = DateTime.UtcNow;
    }
}
