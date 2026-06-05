using CaseBridge_Cases.Data;
using Dapper;
using MediatR;
using System.Net.NetworkInformation;

namespace CaseBridge_Cases.Features.Chat.Queries
{
    public class ValidateChatAccessQuery : IRequest<bool>
    {
        public int CaseId { get; set; }
        public int UserId { get; set; }
        public int? FirmId { get; set; }
        public string Role {  get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
    }

    public class ValidateChatAccessHandler : IRequestHandler<ValidateChatAccessQuery, bool>
    {
        private readonly DapperContext _dapperContext;

        public ValidateChatAccessHandler(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<bool> Handle(ValidateChatAccessQuery request, CancellationToken cancellationToken)
        {
            using var connection = _dapperContext.GetConnection();
            
            return await connection.ExecuteScalarAsync<bool>(
                "sp_ValidateChatAccess",
                new 
                { 
                    CaseId = request.CaseId, 
                    UserId = request.UserId, 
                    FirmId = request.FirmId, 
                    Role = request.Role, 
                    RoomType = request.RoomType 
                },
                commandType: System.Data.CommandType.StoredProcedure
            );

        }
    }
}
