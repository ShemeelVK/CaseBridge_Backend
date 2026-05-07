namespace CaseBridge_Cases.Models
{
    public class CaseDocument
    {
        public int Id { get; set; }

        // Nullable because the document is uploaded BEFORE the case is submitted
        public int? CaseId { get; set; }

        // The ID of the lawyer or client who uploaded it (Prevents stealing!)
        public int UploaderId { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Optional link for real-time chat attachments later
        public int? ChatMessageId { get; set; }
    }
}