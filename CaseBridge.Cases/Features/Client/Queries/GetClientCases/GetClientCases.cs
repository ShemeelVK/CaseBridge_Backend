using MediatR;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using CaseBridge_Cases.DTO;
using Dapper;

namespace CaseBridge_Cases.Features.Client.Queries.GetClientCases
{
    public class GetClientCases : IRequest<IEnumerable<CaseDTO>>
    {
        public int ClientId { get; set; }
    }

    public class GetClientCasesHandler : IRequestHandler<GetClientCases,IEnumerable<CaseDTO>>
    {
        private readonly DapperContext _dapperContext;

        public GetClientCasesHandler(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<IEnumerable<CaseDTO>> Handle(GetClientCases request,CancellationToken cancellationToken)
        {
            using var connection = _dapperContext.GetConnection();

            var sql = @"
                SELECT Id, Title, Description, Status, ClientId, CreatedAt 
                FROM Cases 
                WHERE ClientId = @ClientId 
                ORDER BY CreatedAt DESC";

            return await connection.QueryAsync<CaseDTO>(sql, new { ClientId = request.ClientId });
        }
    }
}
