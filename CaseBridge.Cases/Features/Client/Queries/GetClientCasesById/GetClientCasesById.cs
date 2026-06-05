using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using CaseBridge_Cases.DTO;
using MediatR;
using Dapper;

namespace CaseBridge_Cases.Features.Client.Queries.GetClientCasesById
{
    public class GetClientCasesById : IRequest<CaseDetailDTO>
    {
        public int CaseId { get; set; }
        public int ClientId { get; set; }
    }

    public class GetClientCaseByIdHandler : IRequestHandler<GetClientCasesById, CaseDetailDTO?>
    {
        private readonly DapperContext _dapper;
        public GetClientCaseByIdHandler(DapperContext dapper) => _dapper = dapper;

        public async Task<CaseDetailDTO?> Handle(GetClientCasesById request, CancellationToken ct)
        {
            using var connection = _dapper.GetConnection();
            using var multi = await connection.QueryMultipleAsync(
                "sp_GetClientCaseById", 
                new { request.CaseId, request.ClientId },
                commandType: System.Data.CommandType.StoredProcedure
            );
            var caseDto = await multi.ReadFirstOrDefaultAsync<CaseDetailDTO>();

            if (caseDto != null)
            {
                caseDto.Documents = (await multi.ReadAsync<CaseDocument>()).ToList();
            }
            return caseDto;
        }
    }
}
