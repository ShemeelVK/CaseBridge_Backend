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

            var messages = (await connection.QueryAsync<ChatMessageDTO>(
                "sp_GetChatHistory",
                new 
                { 
                    CaseId = request.CaseId, 
                    RoomType = request.RoomType, 
                    TargetUserId = request.TargetUserId, 
                    CurrentUserId = request.CurrentUserId, 
                    FirmId = request.FirmId 
                },
                commandType: System.Data.CommandType.StoredProcedure
            )).ToList();

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
