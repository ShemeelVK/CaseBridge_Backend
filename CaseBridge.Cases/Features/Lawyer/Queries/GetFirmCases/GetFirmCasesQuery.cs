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
        public int UserId { get; set; }
        public bool IsSenior { get; set; }
    }

    public class GetFirmCaseHandler : IRequestHandler<GetFirmCasesQuery, IEnumerable<CaseDTO>>
    {
        private readonly DapperContext _dapper;
        public GetFirmCaseHandler(DapperContext dapper)
        {
            _dapper = dapper;
        }

        public async Task<IEnumerable<CaseDTO>> Handle(GetFirmCasesQuery request, CancellationToken cancellation)
        {
            using var connection = _dapper.GetConnection();
            return await connection.QueryAsync<CaseDTO>(
                "sp_GetFirmCases", 
                new { FirmId = request.FirmId, UserId = request.UserId, IsSenior = request.IsSenior }, 
                commandType: System.Data.CommandType.StoredProcedure
            );
        }
    }
}
