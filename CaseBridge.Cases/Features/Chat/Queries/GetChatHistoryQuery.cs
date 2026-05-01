using CaseBridge_Cases.DTO;
using CaseBridge_Cases.Data;
using MediatR;
using Dapper;

namespace CaseBridge_Cases.Features.Chat.Queries
{
    public class GetChatHistoryQuery : IRequest<IEnumerable<ChatMessageDTO>>
    {
        public int CaseId { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public int? TargetUserId { get; set; }
        public int? CurrentUserId { get; set; }
        public int? FirmId { get; set; }  // Used to scope messages to the current firm
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
            
            string sql;
            object parameters;

            if (request.CaseId == 0 && request.TargetUserId.HasValue && request.CurrentUserId.HasValue)
            {
                // 1-on-1 DM Logic
                sql = @"
                    SELECT Id, SenderId, SenderName, MessageText, SendAt, ParentMessageId 
                    FROM ChatMessages 
                    WHERE CaseId = 0 AND (
                        (SenderId = @UserId AND ReceiverId = @TargetId) OR 
                        (SenderId = @TargetId AND ReceiverId = @UserId)
                    )
                    ORDER BY SendAt ASC";
                parameters = new { UserId = request.CurrentUserId, TargetId = request.TargetUserId };
            }
            else if (request.FirmId.HasValue)
            {
                // ?? THE LAWYER FIX: Lock strictly to their personal User ID
                // Even if they are in the same firm, the Junior won't see the Senior's messages.
                sql = @"
                    SELECT Id, SenderId, SenderName, MessageText, SendAt, ParentMessageId 
                    FROM ChatMessages 
                    WHERE CaseId = @CaseId AND RoomType = @RoomType 
                    AND (SenderId = @CurrentUserId OR ReceiverId = @CurrentUserId)
                    ORDER BY SendAt ASC";

                parameters = new
                {
                    CaseId = request.CaseId,
                    RoomType = request.RoomType,
                    CurrentUserId = request.CurrentUserId.Value
                };
            }
            else
            {
                // ?? THE CLIENT FIX: Dynamic Isolation
                // Join the Cases table. Only show messages between the Client and the CURRENT Assigned Lawyer.
                sql = @"
                    SELECT m.Id, m.SenderId, m.SenderName, m.MessageText, m.SendAt, m.ParentMessageId 
                    FROM ChatMessages m
                    JOIN Cases c ON m.CaseId = c.Id
                    WHERE m.CaseId = @CaseId AND m.RoomType = @RoomType 
                    AND (
                        (m.SenderId = @CurrentUserId AND m.ReceiverId = c.AcceptedByUserId) OR 
                        (m.SenderId = c.AcceptedByUserId AND m.ReceiverId = @CurrentUserId)
                    )
                    ORDER BY m.SendAt ASC";

                parameters = new
                {
                    CaseId = request.CaseId,
                    RoomType = request.RoomType,
                    CurrentUserId = request.CurrentUserId.Value
                };
            }

            return await connection.QueryAsync<ChatMessageDTO>(sql, parameters);
        }
    }
}
