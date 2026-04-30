using MediatR;
using CaseBridge_Cases.Models;
using CaseBridge_Cases.Data;

using System.Security.Cryptography.X509Certificates;

namespace CaseBridge_Cases.Features.Chat.Commands
{
    public class SendMessage : IRequest<int>
    {
        public int CaseId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }=string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public int? ParentMessageId { get; set; }
    }

    public class SendMessageCommandHandler : IRequestHandler<SendMessage, int>
    {
        private readonly CaseDbContext _context;

        public SendMessageCommandHandler(CaseDbContext context)
        { 
            _context = context;
        }

        public async Task<int> Handle(SendMessage request,CancellationToken cancellationToken)
        {
            var chatMessage = new ChatMessage
            {
                CaseId = request.CaseId,
                SenderId = request.SenderId,
                SenderName = request.SenderName,
                RoomType = request.RoomType,
                MessageText = request.MessageText,
                ParentMessageId = request.ParentMessageId,
                SendAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync(cancellationToken);

            return chatMessage.Id;
        }
    }
}
