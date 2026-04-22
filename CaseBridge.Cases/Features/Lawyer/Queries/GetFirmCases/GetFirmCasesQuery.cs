using MediatR;
using Dapper;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using CaseBridge_Cases.DTO;

namespace CaseBridge_Cases.Features.Lawyer.Queries.GetFirmCases
{
    public class GetFirmCasesQuery : IRequest<IEnumerable<CaseDTO>>
    {
        public int FirmId { get; set; }
    }

    public class GetFirmCaseHandler : IRequestHandler<GetFirmCasesQuery, IEnumerable<CaseDTO>>
    {
        private readonly DapperContext _dapper;
        public GetFirmCaseHandler(DapperContext dapper)
        {
            _dapper = dapper;
        }

        public async Task<IEnumerable<CaseDTO>> Handle(GetFirmCasesQuery requst,CancellationToken cancellation)
        {
            using var connection = _dapper.GetConnection();

            var sql = @"
                SELECT 
                    Id, 
                    ClientId, 
                    Title, 
                    Description, 
                    Category, 
                    Status, 
                    AssignedFirmId, 
                    AcceptedByUserId, 
                    CreatedAt, 
                    LastModifiedByUserId 
                FROM Cases 
                WHERE AssignedFirmId = @FirmId 
                ORDER BY CreatedAt DESC";

            return await connection.QueryAsync<CaseDTO>(sql, new { FirmId = requst.FirmId });
        }
    }
}
