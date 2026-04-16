using MediatR;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using Dapper;
namespace CaseBridge_Cases.Features.Marketplace.Queries.GetOpenCases
{
    public class GetOpenCasesQuery : IRequest<IEnumerable<Case>>
    {
        // For now, it's empty because i just want all open cases.
    }

    //the brain that executes the query
    public class GetOpenCaseHandler : IRequestHandler<GetOpenCasesQuery, IEnumerable<Case>>
    {
        private readonly DapperContext _dapperContext;

        public GetOpenCaseHandler(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<IEnumerable<Case>> Handle(GetOpenCasesQuery request, CancellationToken cancellationToken)
        {
            using var connection = _dapperContext.GetConnection();
            var sql = "SELECT * FROM Cases WHERE Status = 'Open' ORDER BY CreatedAt DESC";

            return await connection.QueryAsync<Case>(sql);
        }
    }
}
