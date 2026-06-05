using MediatR;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using CaseBridge_Cases.DTO;
using Dapper;
namespace CaseBridge_Cases.Features.Marketplace.Queries.GetOpenCases
{
    public class GetOpenCasesQuery : IRequest<IEnumerable<CaseDTO>>
    {
        // For now, it's empty because i just want all open cases.
    }

    //the brain that executes the query
    public class GetOpenCaseHandler : IRequestHandler<GetOpenCasesQuery, IEnumerable<CaseDTO>>
    {
        private readonly DapperContext _dapperContext;

        public GetOpenCaseHandler(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<IEnumerable<CaseDTO>> Handle(GetOpenCasesQuery request, CancellationToken cancellationToken)
        {
            using var connection = _dapperContext.GetConnection();
            return await connection.QueryAsync<CaseDTO>(
                "sp_GetOpenCases", 
                commandType: System.Data.CommandType.StoredProcedure
            );
        }
    }
}
