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

            // THE UNIVERSAL QUERY FIX:
            // Whether it's a Client, a Junior, or a Senior... you only ever see messages 
            // where your exact User ID was the Sender or the Receiver.
            // This perfectly maintains Client Continuity and Ethical Walls.
            if (request.CaseId == 0 && request.TargetUserId.HasValue && request.CurrentUserId.HasValue)
            {
                // 1-on-1 DM Logic (Internal Firm Room)
                sql = @"
                    SELECT Id, SenderId, SenderName, MessageText, SendAt, ParentMessageId 
                    FROM ChatMessages 
                    WHERE CaseId = 0 AND RoomType = @RoomType AND (
                        (SenderId = @UserId AND ReceiverId = @TargetId) OR 
                        (SenderId = @TargetId AND ReceiverId = @UserId)
                    )
                    ORDER BY SendAt ASC";
                parameters = new { RoomType = request.RoomType, UserId = request.CurrentUserId, TargetId = request.TargetUserId };
            }
            else if (!request.TargetUserId.HasValue && request.RoomType.Equals("internal", StringComparison.OrdinalIgnoreCase) && request.FirmId.HasValue)
            {
                // Firm Group Chat Logic (Firm General Room OR Internal Case Room)
                sql = @"
                    SELECT Id, SenderId, SenderName, MessageText, SendAt, ParentMessageId 
                    FROM ChatMessages 
                    WHERE CaseId = @CaseId AND RoomType = @RoomType AND FirmId = @FirmId
                    ORDER BY SendAt ASC";
                parameters = new { CaseId = request.CaseId, RoomType = request.RoomType, FirmId = request.FirmId.Value };
            }
            else
            {
                // Universal External Chat Logic
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

            return await connection.QueryAsync<ChatMessageDTO>(sql, parameters);
        }
    }
}
