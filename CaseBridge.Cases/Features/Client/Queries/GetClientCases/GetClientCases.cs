using MediatR;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using Dapper;

namespace CaseBridge_Cases.Features.Client.Queries.GetClientCases
{
    public class GetClientCases : IRequest<IEnumerable<Case>>
    {
        public int ClientId { get; set; }
    }

    public class GetMyCasesHandler : IRequestHandler<GetClientCases,IEnumerable<Case>>
    {
        private readonly DapperContext _dapperContext;

        public GetMyCasesHandler(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<IEnumerable<Case>> Handle(GetClientCases request,CancellationToken cancellationToken)
        {
            using var connection = _dapperContext.GetConnection();

            var sql= "SELECT * FROM Cases WHERE ClientId = @ClientId ORDER BY CreatedAt DESC";

            return await connection.QueryAsync<Case>(sql, new { ClientId = request.ClientId });
        }
    }
}
