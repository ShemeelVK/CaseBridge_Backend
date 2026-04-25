using CaseBridge_Cases.Models;

namespace CaseBridge_Cases.DTO
{
    public class CaseDTO
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Title { get; set; }= string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Budget {  get; set; }
        public string Status { get; set; } = string.Empty;
        public int? AssignedFirmId { get; set; }
        public int? AcceptedByUserid { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int LastModifiedByUserId { get; set; }
    }
}
