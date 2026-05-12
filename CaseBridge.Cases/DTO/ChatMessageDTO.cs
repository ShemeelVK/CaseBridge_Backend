namespace CaseBridge_Cases.DTO
{
    public class ChatAttachmentDTO
    {
        public int ChatMessageId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public class ChatMessageDTO
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime SendAt { get; set; }
        public int? ParentMessageId { get; set; }
        // Populated in a second Dapper pass — supports multiple files per message
        public List<ChatAttachmentDTO> Attachments { get; set; } = new();
    }
}
