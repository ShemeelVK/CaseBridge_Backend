namespace CaseBridge_Cases.Models
{
    public class CaseHistory
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public Case Case { get; set; } = null!; // EF Navigation Property

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        // The Status Tracking
        public CaseStatus PreviousStatus { get; set; }
        public CaseStatus NewStatus { get; set; }

        // 3. The Audit Information (Who did it and when?)
        public int ModifiedByUserId { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
