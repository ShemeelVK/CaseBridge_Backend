using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

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
            //if roomtype is internal, making sure the user is not client
            string roomName = $"CaseRoom-{caseId}-{roomType}";
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task SendMessage(int caseId,string roomType,string message)
        {
            var userId=Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;

            string roomName = $"CaseRoom-{caseId}-{roomType}";

            // TODO: Call MediatR to save to database:
             //await _mediator.Send(new SendMessageCommand { CaseId = caseId, RoomType = roomType, ... });

            await Clients.Group(roomName).SendAsync("ReceiveMessage",userId,message);
        }
    }
}
