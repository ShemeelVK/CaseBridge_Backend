using CaseBridge_Cases.DTO;
using CaseBridge_Cases.Models;
using CaseBridge_Cases.Data;
using MediatR;
using Dapper;

namespace CaseBridge_Cases.Features.Lawyer.Queries.GetFirmCasesById
{
    public class GetFirmCaseByIdQuery : IRequest<CaseDetailDTO?>
    {
        public int CaseId { get; set; }
        public int FirmId { get; set; }
        public int UserId { get; set; }
        public bool IsSenior { get; set; }
    }

    public class GetFirmCaseByIdHandler : IRequestHandler<GetFirmCaseByIdQuery, CaseDetailDTO?>
    {
        private readonly DapperContext _dapper;
        public GetFirmCaseByIdHandler(DapperContext dapper) => _dapper = dapper;

        public async Task<CaseDetailDTO?> Handle(GetFirmCaseByIdQuery request, CancellationToken ct)
        {
            using var connection = _dapper.GetConnection();

            var caseSql = @"
                        SELECT 
                            Id, ClientId, ClientName, Title, Description, 
                            Status, AssignedFirmId, AcceptedByUserId, CreatedAt, 
                            Category, LastModifiedByUserId, Budget, LawyerName, AiSummary
                        FROM Cases 
                        WHERE Id = @CaseId AND AssignedFirmId = @FirmId";

            if (!request.IsSenior)
            {
                caseSql += " AND AcceptedByUserId = @UserId";
            }

            // Filtered to exclude chat attachments
            var sql = $@"{caseSql}; 
                SELECT 
                    Id, CaseId, UploaderId, FileName, FileUrl, UploadedAt
                FROM CaseDocuments 
                WHERE CaseId = @CaseId AND ChatMessageId IS NULL;";

            using var multi = await connection.QueryMultipleAsync(sql, new
            {
                request.CaseId,
                request.FirmId,
                request.UserId
            });

            var caseDto = await multi.ReadFirstOrDefaultAsync<CaseDetailDTO>();

            if (caseDto != null)
            {
                caseDto.Documents = (await multi.ReadAsync<CaseDocument>()).ToList();
            }
            return caseDto;
        }
    }
}
