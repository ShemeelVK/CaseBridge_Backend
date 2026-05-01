using MediatR;
using CaseBridge_Cases.Models;
using CaseBridge_Cases.Data;


namespace CaseBridge_Cases.Features.Chat.Commands
{
    public class SendMessage : IRequest<int>
    {
        public int CaseId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }=string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public int? ReceiverId { get; set; }
        public int? FirmId { get; set; }
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
            if (request.CaseId > 0 && request.RoomType.Equals("external", StringComparison.OrdinalIgnoreCase))
            {
                // Fetch the case to find out who the current participants are
                var caseObj = await _context.Cases.FindAsync(new object[] { request.CaseId }, cancellationToken);

                if (caseObj != null)
                {
                    // If the Sender is the Client, the Receiver MUST be the active Lawyer
                    if (request.SenderId == caseObj.ClientId)
                    {
                        // Use AcceptedByUserId or AssignedFirmId depending on your exact Cases model properties
                        request.ReceiverId = caseObj.AcceptedByUserId;
                    }
                    // If the Sender is the Lawyer, the Receiver MUST be the Client
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

            return chatMessage.Id;
        }
    }
}
