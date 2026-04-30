using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using CaseBridge_Cases.Features.Chat.Commands;
using CaseBridge_Cases.Features.Chat.Queries;

namespace CaseBridge_Cases.Features.Chat.Hubs
{
    [Authorize]
    public class CaseChatHub : Hub
    {
        private readonly IMediator _mediator;

        public CaseChatHub(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Frontend passes the caseid and the type of room (internal or external)
        public async Task JoinCaseRoom(int caseId, string roomType)
        {

            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var firmId = Context.User?.FindFirst("SeniorId")?.Value;

            if (userId == null || role == null)
            {
                Context.Abort(); // Kicks them out immediately
                return;
            }

            //We ask the database if this person is allowed in.
            //will need to create this simple Dapper query in Queries folder

            var hasAccess = await _mediator.Send(new ValidateChatAccessQuery
            {
                CaseId = caseId,
                UserId = int.Parse(userId),
                FirmId = firmId != null ? int.Parse(firmId) : null,
                Role = role,
                RoomType = roomType
            });

            if (!hasAccess)
            {
                // Send an error message directly back to the person trying to sneak in
                await Clients.Caller.SendAsync("ReceiveSystemMessage", "Access Denied: You must claim this case first.");
                return; // Stop them from joining the group
            }

            //if roomtype is internal, making sure the user is not client
            string roomName = $"CaseRoom-{caseId}-{roomType}";
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task SendMessage(int caseId, string roomType, string message, int? parentMessageId = null)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";

            if (userId == null) return;
            if (string.IsNullOrEmpty(userName)) return;

            int SenderId = int.Parse(userId);

            var command = new SendMessage
            {
                CaseId = caseId,
                SenderId = SenderId,
                SenderName = userName,
                RoomType = roomType,
                MessageText = message,
                ParentMessageId = parentMessageId
            };

            var messageId = await _mediator.Send(command);

            string roomName = $"CaseRoom-{caseId}-{roomType}";

            // Broadcast the message with its ID and parent ID
            await Clients.Group(roomName).SendAsync("ReceiveMessage", new
            {
                id = messageId,
                senderId = SenderId,
                senderName = userName,
                text = message,
                parentMessageId = parentMessageId,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
