using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaseBridge_Contracts
{
    public record DocumentUploadedEvent
    {
        public int DocumentId { get; init; }
        public int CaseId { get; init; }
        public string FileUrl { get; init; }
    }
}
