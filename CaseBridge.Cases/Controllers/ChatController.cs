using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CaseBridge_Cases.DTO;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Features.Chat.Queries;
using System.Security.Claims;
using MediatR;

namespace CaseBridge_Cases.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ChatController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet("cases/{caseId}/chat/{roomType}")]
        public async Task<ActionResult<IEnumerable<ChatMessageDTO>>> GetChatHistory(int caseId,string roomType)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (roomType.Equals("Internal", StringComparison.OrdinalIgnoreCase) && roleClaim == "Client")
            {
                return Forbid(); // Blocks clients from snooping on the lawyer's private chat!
            }

            var query = new GetChatHistoryQuery
            {
                CaseId = caseId,
                RoomType = roomType
            };

            var messages = await _mediator.Send(query);

            return Ok(messages);
        }


    }
}
