using CaseBridge_Cases.Data;
using Dapper;
using MediatR;
using System.Net.NetworkInformation;

namespace CaseBridge_Cases.Features.Chat.Queries
{
    public class ValidateChatAccessQuery : IRequest<bool>
    {
        public int CaseId { get; set; }
        public int UserId { get; set; }
        public int? FirmId { get; set; }
        public string Role {  get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
    }

    public class ValidateChatAccessHandler : IRequestHandler<ValidateChatAccessQuery, bool>
    {
        private readonly DapperContext _dapperContext;

        public ValidateChatAccessHandler(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<bool> Handle(ValidateChatAccessQuery request, CancellationToken cancellationToken)
        {
            using var connection = _dapperContext.GetConnection();

            var sql = @"
                SELECT ClientId, AssignedFirmId 
                FROM Cases 
                WHERE Id = @CaseId";

            //use dynamic typing here since I only need two properties and dont need a full DTO
            var caseOwnership = await connection.QueryFirstOrDefaultAsync(sql, new { request.CaseId });

            if (caseOwnership == null) return false;

            if(request.Role=="Client")
            {
                return caseOwnership.ClientId == request.UserId
                       && request.RoomType.Equals("external", StringComparison.OrdinalIgnoreCase);
            }

            if (request.Role == "Lawyer" || request.Role == "Junior")
            {
                // firm should have claimed the case, and the lawyer should belong to that firm
                // If it passes this, they are allowed in both External and Internal rooms.
                return caseOwnership.AssignedFirmId != null
                       && caseOwnership.AssignedFirmId == request.FirmId;
            }

            return false;

        }
    }
}
