using CaseBridge_Cases.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Security.Claims;

namespace CaseBridge_Cases.Controllers
{
    // OData convention routing: "FirmCases" entity set → FirmCasesController
    // Frontend calls: GET /odata/firm/FirmCases, /odata/firm/FirmCases/$count, etc.
    [Authorize(Roles = "Lawyer,Junior")]
    [Microsoft.AspNetCore.OData.Routing.Attributes.ODataRouteComponent("odata/firm")]
    public class FirmCasesController : ODataController
    {
        private readonly CaseDbContext _context;

        public FirmCasesController(CaseDbContext context)
        {
            _context = context;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            // Same pattern as FirmController.cs line 30-31:
            // JWT uses "SeniorId" for the firm owner ID and "UserId" as the primary user ID claim
            var firmIdClaim = User.FindFirst("SeniorId")?.Value;
            var userIdClaim = User.FindFirst("UserId")?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(firmIdClaim) || string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { Message = "Firm ID or User ID is missing from your security token." });
            }

            if (!int.TryParse(firmIdClaim, out int firmId))
            {
                return Unauthorized(new { Message = "Invalid Firm ID in security token." });
            }

            // SECURITY: Pre-filter so a lawyer only ever sees their firm's cases.
            return Ok(_context.Cases.Where(c => c.AssignedFirmId == firmId));
        }
    }
}
