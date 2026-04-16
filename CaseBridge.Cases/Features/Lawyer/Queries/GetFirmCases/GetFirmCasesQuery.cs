using MediatR;
using Dapper;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;

namespace CaseBridge_Cases.Features.Lawyer.Queries.GetFirmCases
{
    public class GetFirmCasesQuery : IRequest<IEnumerable<Case>>
    {
        public int FirmId { get; set; }
    }

    public class GetFirmCaseHandler : IRequestHandler<GetFirmCasesQuery, IEnumerable<Case>>
    {
        private readonly DapperContext _dapper;
        public GetFirmCaseHandler(DapperContext dapper)
        {
            _dapper = dapper;
        }

        public async Task<IEnumerable<Case>> Handle(GetFirmCasesQuery requst,CancellationToken cancellation)
        {
            using var connection = _dapper.GetConnection();

            var sql = "SELECT * FROM Cases WHERE AssignedFirmId=@FirmId ORDER BY CreatedAt DESC";

            return await connection.QueryAsync<Case>(sql, new { FirmId = requst.FirmId });
        }
    }
}
