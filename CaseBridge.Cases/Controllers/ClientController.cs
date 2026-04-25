using CaseBridge_Cases.Data;
using CaseBridge_Cases.Features.Client.Command.PostCase;
using CaseBridge_Cases.Features.Client.Queries.GetClientCases;
using CaseBridge_Cases.Models;
using CaseBridge_Cases.DTO;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Collections;

namespace CaseBridge_Cases.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles ="Client")]
    public class ClientController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ClientController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("post-case")]
        public async Task<IActionResult> PostNewCase([FromBody] PostCaseCommand command)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { Message = "User ID is missing from your security token." });
            }

            command.ClientId=int.Parse(userIdClaim);

            var newCaseId = await _mediator.Send(command);
            return Ok(new { Message = "Case posted successfully to the marketplace!", CaseId = newCaseId });
        }

        [HttpGet("get-cases")]
        public async Task<ActionResult<IEnumerable<CaseDTO>>> GetClientCases()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { Message = "User ID is missing from your security token." });
            }

            int secureClientId = int.Parse(userIdClaim);

            var cases = await _mediator.Send(new GetClientCases { ClientId = secureClientId });

            return Ok(cases);
        }
    }
}
