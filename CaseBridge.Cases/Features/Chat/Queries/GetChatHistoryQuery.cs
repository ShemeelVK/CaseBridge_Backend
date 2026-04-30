using CaseBridge_Cases.DTO;
using CaseBridge_Cases.Data;
using MediatR;
using Dapper;
namespace CaseBridge_Cases.Features.Chat.Queries
{
    public class GetChatHistoryQuery : IRequest<IEnumerable<ChatMessageDTO>>
    {
        public int CaseId { get; set; }
        public string RoomType { get; set; }
    }

    public class GetChatHistoryHandler : IRequestHandler<GetChatHistoryQuery, IEnumerable<ChatMessageDTO>>
    {
        private readonly DapperContext _context;

        public GetChatHistoryHandler(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ChatMessageDTO>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
        {
            using var connection = _context.GetConnection();

            var sql = @"
                SELECT 
                    Id, 
                    SenderId, 
                    SenderName, 
                    MessageText, 
                    SendAt,
                    ParentMessageId
                FROM ChatMessages 
                WHERE CaseId = @CaseId AND RoomType = @RoomType 
                ORDER BY SendAt ASC";

            return await connection.QueryAsync<ChatMessageDTO>(sql, new { CaseId = request.CaseId, RoomType = request.RoomType });
        }
    }
}
