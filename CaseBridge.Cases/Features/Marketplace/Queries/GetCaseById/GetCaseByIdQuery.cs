using MediatR;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using Dapper;

namespace CaseBridge_Cases.Features.Marketplace.Queries.GetCaseById
{
    public class GetCaseByIdQuery : IRequest<Case?>
    {
        public int Id { get; set; }
    }

    public class GetCaseByIdHandler : IRequestHandler<GetCaseByIdQuery, Case?>
    {
        private readonly DapperContext _dapperContext;

        public GetCaseByIdHandler(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<Case?> Handle(GetCaseByIdQuery request, CancellationToken cancellationToken)
        {
            using var connection = _dapperContext.GetConnection();
            var sql = @"
                SELECT 
                    Id, 
                    ClientId, 
                    Title, 
                    Description, 
                    Category, 
                    Status,
                    Budget,
                    AssignedFirmId, 
                    AcceptedByUserId, 
                    CreatedAt, 
                    LastModifiedByUserId 
                FROM Cases 
                WHERE Id = @Id";

            return await connection.QueryFirstOrDefaultAsync<Case>(sql, new {request.Id});
        }
    }
}
