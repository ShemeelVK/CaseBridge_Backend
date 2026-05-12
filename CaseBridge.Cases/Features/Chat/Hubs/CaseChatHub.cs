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
            int? firmId = string.IsNullOrEmpty(firmIdStr) ? null : int.Parse(firmIdStr);
            string roomName;

            if (caseId == 0)
            {
                if (targetUserId.HasValue)
                {
                    int id1 = Math.Min(userId, targetUserId.Value);
                    int id2 = Math.Max(userId, targetUserId.Value);
                    roomName = $"DM-{id1}-{id2}";
                }
                else
                {
                    if (firmId == null) { Context.Abort(); return; }
                    roomName = $"FirmRoom-{firmId}";
                }
            }
            else
            {
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

        public async Task SendMessage(int caseId, string roomType, string message, int? targetUserId = null, int? parentMessageId = null, int[]? attachmentDocIds = null)
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("UserId")?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? Context.User?.FindFirst("name")?.Value ?? "Unknown User";
            var firmIdStr = Context.User?.FindFirst("SeniorId")?.Value;

            if (userIdStr == null || string.IsNullOrEmpty(userName)) return;

            int userId = int.Parse(userIdStr);
            string roomName;

            if (caseId == 0)
            {
                if (targetUserId.HasValue)
                {
                    int id1 = Math.Min(userId, targetUserId.Value);
                    int id2 = Math.Max(userId, targetUserId.Value);
                    roomName = $"DM-{id1}-{id2}";
                }
                else
                {
                    if (string.IsNullOrEmpty(firmIdStr)) { Context.Abort(); return; }
                    roomName = $"FirmRoom-{firmIdStr}";
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
                FirmId = string.IsNullOrEmpty(firmIdStr) ? null : int.Parse(firmIdStr),
                RoomType = roomType,
                MessageText = message,
                ParentMessageId = parentMessageId,
                AttachmentDocIds = attachmentDocIds?.ToList()
            };

            var messageId = await _mediator.Send(command);

            // Resolve attachment info for real-time broadcast using a scoped context
            var attachments = new List<object>();
            if (attachmentDocIds is { Length: > 0 })
            {
                using var scope = Context.GetHttpContext()!.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Data.CaseDbContext>();
                var docs = db.CaseDocuments
                    .Where(d => attachmentDocIds.Contains(d.Id))
                    .Select(d => new { fileUrl = d.FileUrl, fileName = d.FileName })
                    .ToList();

                attachments.AddRange(docs);
            }

            await Clients.Group(roomName).SendAsync("ReceiveMessage", new
            {
                id = messageId,
                caseId = caseId,
                senderId = userId,
                senderName = userName,
                text = message,
                parentMessageId = parentMessageId,
                attachments = attachments,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
