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
                // Is the case currently assigned to this firm?
                bool isCurrentlyAssigned = caseOwnership.AssignedFirmId != null && caseOwnership.AssignedFirmId == request.FirmId;

                if (isCurrentlyAssigned)
                {
                    return true; // Allowed in both External and Internal rooms
                }

                // Historical Bypass: If they dropped it, check if they have past messages
                var pastMessagesSql = @"
                    SELECT COUNT(1) 
                    FROM ChatMessages 
                    WHERE CaseId = @CaseId 
                    AND (FirmId = @FirmId OR SenderId = @UserId OR ReceiverId = @UserId)";
                    
                var pastMessagesCount = await connection.ExecuteScalarAsync<int>(pastMessagesSql, new { request.CaseId, request.FirmId, request.UserId });
                
                return pastMessagesCount > 0;
            }

            return false;

        }
    }
}
