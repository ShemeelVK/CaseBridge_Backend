using CaseBridge_Cases.Models;

namespace CaseBridge_Cases.DTO
{
        // Inherits all properties (Id, Title, Budget, etc.) from CaseDTO!
        public class CaseDetailDTO : CaseDTO
        {
            // Add ONLY the extra stuff needed for the detail view
            public List<CaseDocument> Documents { get; set; } = new List<CaseDocument>();
        }
}
