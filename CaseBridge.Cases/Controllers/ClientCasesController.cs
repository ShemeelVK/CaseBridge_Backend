using CaseBridge_Cases.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Security.Claims;

namespace CaseBridge_Cases.Controllers
{
    // OData convention routing: "ClientCases" entity set → ClientCasesController
    // Frontend calls: GET /odata/client/ClientCases, /odata/client/ClientCases/$count, etc.
    [Authorize(Roles = "Client")]
    [Microsoft.AspNetCore.OData.Routing.Attributes.ODataRouteComponent("odata/client")]
    public class ClientCasesController : ODataController
    {
        private readonly CaseDbContext _context;

        public ClientCasesController(CaseDbContext context)
        {
            _context = context;
        }

        // [EnableQuery] intercepts the IQueryable BEFORE executing SQL.
        // OData reads the URL (?$filter=, ?$top=, etc.) and appends them to
        // the query, then EF Core generates one optimized SQL statement.
        [EnableQuery]
        public IActionResult Get()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int clientId))
            {
                return Unauthorized(new { Message = "Invalid or missing user ID in token." });
            }

            // SECURITY: Client can ONLY ever see their own cases regardless of
            // whatever $filter the frontend sends. OData appends on top of this.
            return Ok(_context.Cases.Where(c => c.ClientId == clientId));
        }
    }
}
