using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Features.Chat.Commands;
using CaseBridge_Cases.Features.Chat.Queries;

namespace CaseBridge_Cases.Features.Chat.Hubs
{
    [Authorize]
    public class CaseChatHub : Hub
    {
        private readonly IMediator _mediator;
        private readonly CaseDbContext _context;

        public CaseChatHub(IMediator mediator, CaseDbContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        // Join a personal group so call invites can be targeted to this user
        public override async Task OnConnectedAsync()
        {
            var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("UserId")?.Value;
            if (userIdStr != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userIdStr}");
            await base.OnConnectedAsync();
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

        public async Task SendMessage(int caseId, string roomType, string message, int? targetUserId = null, int? parentMessageId = null, List<int>? attachmentDocIds = null)
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
                ParentMessageId = parentMessageId,
                AttachmentDocIds = attachmentDocIds
            };

            var result = await _mediator.Send(command);

            await Clients.Group(roomName).SendAsync("ReceiveMessage", new
            {
                id = result.MessageId,
                caseId = caseId,
                senderId = userId,
                senderName = userName,
                text = message,
                parentMessageId = parentMessageId,
                timestamp = DateTime.UtcNow,
                attachments = result.Attachments.Select(a => new { fileUrl = a.FileUrl, fileName = a.FileName }).ToList()
            });
        }

        // ── Video / Audio Call Signaling ─────────────────────────────────────────

        public async Task InitiateCall(int caseId, string roomType, string roomName, string callType, int? explicitTargetUserId = null)
        {
            // Try multiple claim name variants — auth services vary in which one they set
            var callerName = Context.User?.FindFirst("FullName")?.Value
                          ?? Context.User?.FindFirst("fullName")?.Value
                          ?? Context.User?.FindFirst("name")?.Value
                          ?? Context.User?.FindFirst("unique_name")?.Value
                          ?? Context.User?.FindFirst(ClaimTypes.Name)?.Value
                          ?? Context.User?.FindFirst(ClaimTypes.Email)?.Value
                          ?? "User";
            var callerIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? Context.User?.FindFirst("UserId")?.Value;
            if (callerIdStr == null) return;
            int callerId = int.Parse(callerIdStr);

            int resolvedTargetId;

            if (explicitTargetUserId.HasValue && explicitTargetUserId.Value > 0)
            {
                // DM / internal: target is explicitly provided
                resolvedTargetId = explicitTargetUserId.Value;
            }
            else if (caseId > 0)
            {
                // External case chat: resolve the other participant from the DB
                var caseObj = await _context.Cases.FindAsync(caseId);
                if (caseObj == null) return;

                // Caller is the client → call the lawyer; caller is the lawyer → call the client
                resolvedTargetId = callerId == caseObj.ClientId
                    ? (caseObj.AcceptedByUserId ?? 0)
                    : caseObj.ClientId;

                if (resolvedTargetId == 0) return; // Case not yet assigned
            }
            else return;

            // Notify the receiver
            await Clients.Group($"user-{resolvedTargetId}").SendAsync("IncomingCall", new
            {
                roomName, callType, callerName, callerId, caseId
            });

            // Notify the CALLER with the resolved target so frontend can open the modal
            await Clients.Caller.SendAsync("CallInitiated", new
            {
                roomName, callType, resolvedTargetId
            });
        }

        public async Task AcceptCall(string roomName, int callerId)
        {
            await Clients.Group($"user-{callerId}").SendAsync("CallAccepted", new { roomName });
        }

        public async Task RejectCall(string roomName, int callerId)
        {
            await Clients.Group($"user-{callerId}").SendAsync("CallRejected", new { roomName });
        }

        // ── WebRTC Relay ──────────────────────────────────────────────────────────

        public async Task RelayOffer(string roomName, int targetUserId, string sdp)
        {
            await Clients.Group($"user-{targetUserId}").SendAsync("ReceiveOffer", new { roomName, sdp });
        }

        public async Task RelayAnswer(string roomName, int targetUserId, string sdp)
        {
            await Clients.Group($"user-{targetUserId}").SendAsync("ReceiveAnswer", new { roomName, sdp });
        }

        public async Task RelayIceCandidate(string roomName, int targetUserId, string candidate)
        {
            await Clients.Group($"user-{targetUserId}").SendAsync("ReceiveIceCandidate", new { roomName, candidate });
        }
        public async Task EndCall(string roomName, int targetUserId)
        {
            await Clients.Group($"user-{targetUserId}").SendAsync("CallEnded", new { roomName });
        }

        // Sent when the receiver is already on another call — gives caller a "busy" signal
        public async Task BusyReject(string roomName, int callerId)
        {
            await Clients.Group($"user-{callerId}").SendAsync("CallBusy", new { roomName });
        }

        // Tells the held person they've been put on / resumed from hold
        public async Task NotifyHold(string roomName, int targetUserId, bool isOnHold)
        {
            await Clients.Group($"user-{targetUserId}").SendAsync("CallHoldChanged", new { roomName, isOnHold });
        }
    }
}
