using MediatR;
using Dapper;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.DTO;

namespace CaseBridge_Cases.Features.Chat.Queries
{
    public class GetChatCasesQuery : IRequest<IEnumerable<CaseDTO>>
    {
        public int FirmId { get; set; }
        public int UserId { get; set; }
        public bool IsSenior { get; set; }
    }

    public class GetChatCasesHandler : IRequestHandler<GetChatCasesQuery, IEnumerable<CaseDTO>>
    {
        private readonly DapperContext _dapper;
        
        public GetChatCasesHandler(DapperContext dapper)
        {
            _dapper = dapper;
        }

        public async Task<IEnumerable<CaseDTO>> Handle(GetChatCasesQuery request, CancellationToken cancellation)
        {
            using var connection = _dapper.GetConnection();

            var sql = @"
                SELECT 
                    Id, 
                    ClientId, 
                    ClientName,
                    Title, 
                    Description, 
                    Category, 
                    Status,
                    Budget,
                    AssignedFirmId, 
                    AcceptedByUserId, 
                    LawyerName,
                    CreatedAt, 
                    LastModifiedByUserId 
                FROM Cases 
                WHERE (AssignedFirmId = @FirmId";

            if (!request.IsSenior)
            {
                sql += " AND AcceptedByUserId = @UserId)";
            }
            else
            {
                sql += ")";
            }

            // Include cases where the firm/lawyer has past chat messages
            // For seniors, we check FirmId but also fallback to SenderId/ReceiverId for legacy messages (before FirmId was added)
            if (request.IsSenior)
            {
                sql += " OR Id IN (SELECT CaseId FROM ChatMessages WHERE FirmId = @FirmId OR SenderId = @UserId OR ReceiverId = @UserId)";
            }
            else
            {
                sql += " OR Id IN (SELECT CaseId FROM ChatMessages WHERE SenderId = @UserId OR ReceiverId = @UserId)";
            }

            sql += " ORDER BY CreatedAt DESC";

            return await connection.QueryAsync<CaseDTO>(sql, new { FirmId = request.FirmId, UserId = request.UserId });
        }
    }
}
