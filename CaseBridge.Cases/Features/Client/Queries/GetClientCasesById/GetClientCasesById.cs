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
                    var sql = @"
                SELECT 
                    Id, ClientId, ClientName, Title, Description, 
                    Status, AssignedFirmId, AcceptedByUserId, CreatedAt, 
                    Category, LastModifiedByUserId, Budget, LawyerName
                FROM Cases 
                WHERE Id = @CaseId AND ClientId = @ClientId;

                SELECT 
                    Id, CaseId, UploaderId, FileName, FileUrl, UploadedAt
                FROM CaseDocuments 
                WHERE CaseId = @CaseId AND ChatMessageId IS NULL;";

            using var multi = await connection.QueryMultipleAsync(sql, new { request.CaseId, request.ClientId });
            var caseDto = await multi.ReadFirstOrDefaultAsync<CaseDetailDTO>();

            if (caseDto != null)
            {
                caseDto.Documents = (await multi.ReadAsync<CaseDocument>()).ToList();
            }
            return caseDto;
        }
    }
}
