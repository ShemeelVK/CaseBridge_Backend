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

        public async Task JoinCaseRoom(int caseId, string roomType = "external", int? targetUserId = null)
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("UserId")?.Value;
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? Context.User?.FindFirst("role")?.Value;
            var firmIdStr = Context.User?.FindFirst("SeniorId")?.Value;

            if (userIdStr == null || role == null)
            {
                Context.Abort();
                return;
            }

            int userId = int.Parse(userIdStr);
            int? firmId=string.IsNullOrEmpty(firmIdStr) ? null : int.Parse(firmIdStr);
            string roomName;

            if (caseId == 0)
            {
                if (targetUserId.HasValue)
                {
                    // 1-on-1 DM: Use a unique room name for these two users
                    int id1 = Math.Min(userId, targetUserId.Value);
                    int id2 = Math.Max(userId, targetUserId.Value);
                    roomName = $"DM-{id1}-{id2}";
                }
                else
                {
                    if(firmId==null)
                    {
                        Context.Abort();
                        return;
                    }
                    // Firm-wide general chat
                    roomName = $"FirmRoom-{firmId}";
                }
            }
            else
            {
                // Case-specific chat
                roomName = $"CaseRoom-{caseId}-{roomType}";

                var hasAccess = await _mediator.Send(new ValidateChatAccessQuery
                {
                    CaseId = caseId,
                    UserId = userId,
                    FirmId = firmId,
                    Role = role,
                    RoomType = roomType
                });

                if (!hasAccess)
                {
                    await Clients.Caller.SendAsync("ReceiveSystemMessage", "Access Denied: You are not authorized for this case chat.");
                    return;
                }
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task SendMessage(int caseId, string roomType, string message, int? targetUserId = null, int? parentMessageId = null)
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("UserId")?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? Context.User?.FindFirst("name")?.Value ?? "Unknown User";
            var firmId = Context.User?.FindFirst("SeniorId")?.Value;

            if (userIdStr == null || string.IsNullOrEmpty(userName)) return;

            int userId = int.Parse(userIdStr);
            string roomName;

            if (caseId == 0 && firmId==null)
            {
                if (targetUserId.HasValue)
                {
                    int id1 = Math.Min(userId, targetUserId.Value);
                    int id2 = Math.Max(userId, targetUserId.Value);
                    roomName = $"DM-{id1}-{id2}";
                }
                else
                {
                    if(firmId==null)
                    {
                        Context.Abort();
                        return;
                    }
                    roomName = $"FirmRoom-{firmId}";
                }
            }
            else
            {
                roomName = $"CaseRoom-{caseId}-{roomType}";
            }

            var command = new SendMessage
            {
                CaseId = caseId,
                SenderId = userId,
                SenderName = userName,
                ReceiverId = targetUserId,
                FirmId = string.IsNullOrEmpty(firmId) ? null : int.Parse(firmId),
                RoomType = roomType,
                MessageText = message,
                ParentMessageId = parentMessageId
            };

            var messageId = await _mediator.Send(command);

            await Clients.Group(roomName).SendAsync("ReceiveMessage", new
            {
                id = messageId,
                caseId = caseId,
                senderId = userId,
                senderName = userName,
                text = message,
                parentMessageId = parentMessageId,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
