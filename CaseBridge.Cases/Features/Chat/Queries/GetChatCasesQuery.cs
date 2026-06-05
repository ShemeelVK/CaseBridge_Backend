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
            return await connection.QueryAsync<CaseDTO>(
                "sp_GetChatCases", 
                new { FirmId = request.FirmId, UserId = request.UserId, IsSenior = request.IsSenior }, 
                commandType: System.Data.CommandType.StoredProcedure
            );
        }
    }
}
