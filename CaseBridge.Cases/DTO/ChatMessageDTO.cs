namespace CaseBridge_Cases.DTO
{
    public class ChatMessageDTO
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime SendAt { get; set; }
        public int? ParentMessageId { get; set; }
    }
}
