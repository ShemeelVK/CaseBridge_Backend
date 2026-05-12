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
        public int? FirmId { get; set; }
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
                // 1-on-1 DM
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
                // Firm group chat
                sql = @"
                    SELECT Id, SenderId, SenderName, MessageText, SendAt, ParentMessageId
                    FROM ChatMessages
                    WHERE CaseId = @CaseId AND RoomType = @RoomType AND FirmId = @FirmId
                    ORDER BY SendAt ASC";
                parameters = new { CaseId = request.CaseId, RoomType = request.RoomType, FirmId = request.FirmId.Value };
            }
            else
            {
                // Universal external chat
                sql = @"
                    SELECT Id, SenderId, SenderName, MessageText, SendAt, ParentMessageId
                    FROM ChatMessages
                    WHERE CaseId = @CaseId AND RoomType = @RoomType
                    AND (SenderId = @CurrentUserId OR ReceiverId = @CurrentUserId)
                    ORDER BY SendAt ASC";
                parameters = new { CaseId = request.CaseId, RoomType = request.RoomType, CurrentUserId = request.CurrentUserId!.Value };
            }

            var messages = (await connection.QueryAsync<ChatMessageDTO>(sql, parameters)).ToList();

            // Second pass: fetch all attachments for these messages in one query
            if (messages.Count > 0)
            {
                var messageIds = messages.Select(m => m.Id).ToList();

                const string attachmentSql = @"
                    SELECT ChatMessageId, FileUrl, FileName
                    FROM CaseDocuments
                    WHERE ChatMessageId IN @Ids";

                var attachments = await connection.QueryAsync<ChatAttachmentDTO>(attachmentSql, new { Ids = messageIds });

                // Group by message and assign
                var grouped = attachments.GroupBy(a => a.ChatMessageId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var msg in messages)
                {
                    if (grouped.TryGetValue(msg.Id, out var docs))
                        msg.Attachments = docs;
                }
            }

            return messages;
        }
    }
}
