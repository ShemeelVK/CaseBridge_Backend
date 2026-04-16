using CaseBridge_Cases.Data;
using CaseBridge_Cases.Features.Lawyer.Commands.CloseCase;
using CaseBridge_Cases.Features.Lawyer.Queries.GetFirmCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CaseBridge_Cases.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class FirmController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FirmController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("lawyer-cases")]
        public async Task<IActionResult> GetMyFirmCases()
        {
            var firmIdClaim = User.FindFirst("SeniorId")?.Value;

            if (string.IsNullOrEmpty(firmIdClaim))
            {
                return Unauthorized(new { Message = "Firm ID is missing from your security token." });
            }

            int secureFirmId = int.Parse(firmIdClaim);

            var cases = await _mediator.Send(new GetFirmCasesQuery { FirmId = secureFirmId });

            return Ok(cases);

        }

        [HttpPut("cases/{id}/close-case")]
        public async Task<IActionResult> CloseCase(int id)
        {
            var seniorIdClaim = User.FindFirst("SeniorId")?.Value;

            if (string.IsNullOrEmpty(seniorIdClaim))
            {
                return Unauthorized(new { Message = "Senior ID is missing from your security token." });
            }

            var command = new CloseCaseCommand
            {
                CaseId = id,
                FirmId = int.Parse(seniorIdClaim)
            };

            try
            {
                var result=await _mediator.Send(command);
                return Ok(new { Message = "Case successfully closed!", Success = result });

            }
            catch (UnauthorizedAccessException ex)
            {
                // Return 403 Forbidden if they try to close someone else's case
                return StatusCode(403, new { Error = ex.Message });
            }

            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            
        }

    }
}
