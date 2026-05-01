namespace CaseBridge_Cases.Models
{
    public class Case
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal Budget { get; set; }
        public CaseStatus Status { get; set; } = CaseStatus.Open;
        public int? AssignedFirmId { get; set; }
        public int? AcceptedByUserId{ get; set; }
        public string LawyerName { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public int LastModifiedByUserId { get; set; }

        public ICollection<CaseHistory> Histories { get; set; } = new List<CaseHistory>();
    }
}
