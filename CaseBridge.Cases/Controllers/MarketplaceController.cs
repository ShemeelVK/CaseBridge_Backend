using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CaseBridge_Cases.Features.Marketplace.Queries.GetOpenCases;
using CaseBridge_Cases.Features.Marketplace.Queries.GetCaseById;
using CaseBridge_Cases.Features.Marketplace.Commands.ClaimCase;
using MediatR;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace CaseBridge_Cases.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketplaceController : ControllerBase
    {
        private readonly IMediator _mediator;
        public MarketplaceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("cases")]
        public async Task<IActionResult> GetMarketPlaceFeed()
        {
            var cases = await _mediator.Send(new GetOpenCasesQuery());

            return Ok(cases);
        }

        [HttpGet("cases/{id}")]
        public async Task<IActionResult> GetCaseById(int id)
        {
            var caseDetails= await _mediator.Send(new GetCaseByIdQuery { Id = id });

            if(caseDetails==null)
            {
                return NotFound(new { Message = $"Case with ID {id} was not found." });
            }
            return Ok(caseDetails);
        }

        [HttpPut("cases/{id}/claim")]
        [Authorize(Roles ="Lawyer,Junior")]
        public async Task<IActionResult> ClaimCase(int id)
        {
            var lawyerIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // Check the exact claim name used in the TokenService first
            var userNameClaim = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value 
                              ?? User.FindFirst("name")?.Value 
                              ?? User.FindFirst(ClaimTypes.Name)?.Value;
            var seniorIdClaim = User.FindFirst("SeniorId")?.Value;

            if (string.IsNullOrEmpty(lawyerIdClaim) || string.IsNullOrEmpty(seniorIdClaim))
            {
                return Unauthorized(new { Message = "Required user or firm IDs are missing from your security token." });
            }

            var command = new ClaimCaseCommand
            {
                CaseId = id,
                LawyerId = int.Parse(lawyerIdClaim),
                FirmId = int.Parse(seniorIdClaim),
                LawyerName = userNameClaim ?? "Unknown Lawyer"
            };

            try
            {
                var result = await _mediator.Send(command);

                return Ok(new { Message = "Case successfully claimed!", Success = result });
            }
            catch(Exception ex) 
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
