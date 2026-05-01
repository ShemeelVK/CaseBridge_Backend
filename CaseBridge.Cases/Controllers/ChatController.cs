using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CaseBridge_Cases.DTO;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Features.Chat.Queries;
using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CaseBridge_Cases.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly CaseDbContext _dbContext;

        public ChatController(IMediator mediator, CaseDbContext dbContext)
        {
            _mediator = mediator;
            _dbContext = dbContext;
        }

        [HttpGet("cases/{caseId}/chat/{roomType}")]
        public async Task<ActionResult<IEnumerable<ChatMessageDTO>>> GetChatHistory(int caseId, string roomType, [FromQuery] int? targetUserId = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            var seniorIdClaim = User.FindFirst("SeniorId")?.Value;

            if (userIdClaim == null) return Unauthorized();
            int currentUserId = int.Parse(userIdClaim);

            // Access control for Internal chat
            if (roomType.Equals("Internal", StringComparison.OrdinalIgnoreCase) && roleClaim == "Client")
            {
                return Forbid(); 
            }

            // Security: For External chats, verify the firm/client assignment
            if (roomType.Equals("External", StringComparison.OrdinalIgnoreCase))
            {
                var caseObj = await _dbContext.Cases.FindAsync(caseId);
                if (caseObj == null) return NotFound();

                if (roleClaim == "Client")
                {
                    if (caseObj.ClientId != currentUserId) return Forbid();
                }
                else
                {
                    // Lawyer/Junior must belong to the firm that claimed the case
                    int seniorId = seniorIdClaim != null ? int.Parse(seniorIdClaim) : currentUserId;
                    if (caseObj.AssignedFirmId == null || caseObj.AssignedFirmId != seniorId)
                        return Forbid();
                }
            }

            var query = new GetChatHistoryQuery
            {
                CaseId = caseId,
                RoomType = roomType,
                CurrentUserId = currentUserId,
                TargetUserId = targetUserId,
                FirmId = seniorIdClaim != null ? int.Parse(seniorIdClaim) : (int?)null
            };

            var messages = await _mediator.Send(query);
            return Ok(messages);
        }
    }
}
