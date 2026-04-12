using CaseBridge_Users.DTOs;
using CaseBridge_Users.Models;
using CaseBridge_Users.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CaseBridge_Users.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles ="Junior")]
    public class JuniorController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        public JuniorController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet("my-senior")]
        public async Task<IActionResult> GetMySenior()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int juniorId))
                return Unauthorized();

            var seniorInfo = await _userRepository.GetSeniorForJuniorAsync(juniorId);

            if(seniorInfo == null)
                return NotFound("No Senior Advocate found for this account.");

            return Ok(seniorInfo);
        }
    }
}
