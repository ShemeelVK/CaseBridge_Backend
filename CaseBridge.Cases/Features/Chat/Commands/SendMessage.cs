using MediatR;
using CaseBridge_Cases.Models;
using CaseBridge_Cases.Data;

namespace CaseBridge_Cases.Features.Chat.Commands
{
    public class SendMessage : IRequest<int>
    {
        public int CaseId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public int? ReceiverId { get; set; }
        public int? FirmId { get; set; }
        public int? ParentMessageId { get; set; }
        // Optional: IDs of pre-uploaded CaseDocuments to attach to this message
        public List<int>? AttachmentDocIds { get; set; }
    }

    public class SendMessageCommandHandler : IRequestHandler<SendMessage, int>
    {
        private readonly CaseDbContext _context;

        public SendMessageCommandHandler(CaseDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(SendMessage request, CancellationToken cancellationToken)
        {
            if (request.CaseId > 0 && request.RoomType.Equals("external", StringComparison.OrdinalIgnoreCase))
            {
                var caseObj = await _context.Cases.FindAsync(new object[] { request.CaseId }, cancellationToken);

                if (caseObj != null)
                {
                    if (request.SenderId == caseObj.ClientId)
                    {
                        if (caseObj.AcceptedByUserId == null)
                            throw new InvalidOperationException("Cannot send messages until a lawyer has claimed the case.");
                        request.ReceiverId = caseObj.AcceptedByUserId;
                    }
                    else
                    {
                        request.ReceiverId = caseObj.ClientId;
                    }
                }
            }

            var chatMessage = new ChatMessage
            {
                CaseId = request.CaseId,
                SenderId = request.SenderId,
                SenderName = request.SenderName,
                RoomType = request.RoomType,
                MessageText = request.MessageText,
                ReceiverId = request.ReceiverId,
                FirmId = request.FirmId,
                ParentMessageId = request.ParentMessageId,
                SendAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync(cancellationToken);

            // Link each pre-uploaded document to this chat message
            if (request.AttachmentDocIds is { Count: > 0 })
            {
                var docs = _context.CaseDocuments
                    .Where(d => request.AttachmentDocIds.Contains(d.Id))
                    .ToList();

                foreach (var doc in docs)
                {
                    doc.ChatMessageId = chatMessage.Id;
                    if (doc.CaseId == null) doc.CaseId = request.CaseId > 0 ? request.CaseId : null;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            return chatMessage.Id;
        }
    }
}
