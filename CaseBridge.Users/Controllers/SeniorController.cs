using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CaseBridge_Users.DTOs;
using CaseBridge_Users.Models;
using CaseBridge_Users.Repositories;
using CaseBridge_Users.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using CaseBridge_Users.DTOs.Firm;

namespace CaseBridge_Users.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles ="Lawyer,Junior")]
    public class SeniorController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly EmailService _emailService;
        public SeniorController(UserRepository userRepository, EmailService emailService)
        {
            _userRepository = userRepository;
            _emailService = emailService;
        }

        [HttpPost("add-junior")]
        public async Task<IActionResult> AddJuniorAssociate([FromBody] AddJuniorDto dto)
        {
            //extract id from token
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int callerId))
            {
                return Unauthorized("Invalid Token Data");
            }

            //the caller is actually a SENIOR
            var (callerUser, callerProfile) = await _userRepository.GetUserAndProfileAsync(callerId);

            if(callerUser?.UserType != "Lawyer" || callerProfile?.SeniorLawyerId != null)
            {
                return StatusCode(403, "Only Firm Owners (Senior Advocates) can add Junior Associates.");
            }

            //map the data and assign the Caller's ID as the BOSS
            var newJuniorUser = new User
            {
                Email = dto.Email,
                FullName = dto.FullName,
                UserType = "Junior" //hardcoded
            };

            var newJuniorProfile = new LawyerProfile
            {
                EnrollmentNumber = dto.EnrollmentNumber,
                Specialization = dto.Specialization,
                SeniorLawyerId = callerId //link locking the junior to this senior
            };

            var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            var success = await _userRepository.RegisterLawyerAsync(newJuniorUser, newJuniorProfile, dto.TemporaryPassword, verificationToken);

            if(!success)
            {
                return BadRequest("Failed to add junior. Email or Enrollment number may already exist");
            }

            await _emailService.SendVerificationEmailAsync(dto.Email, dto.FullName, verificationToken);

            return Ok(new {Message= "Junior Associate successfully added to your firm." });
        }

        [HttpGet("associates")]
        public async Task<IActionResult> GetMyAssociates()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var seniorIdClaim = User.FindFirst("SeniorId");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int callerId))
                return Unauthorized();

            // If they have a SeniorId claim, use it. Otherwise, they ARE the senior.
            int seniorId = seniorIdClaim != null ? int.Parse(seniorIdClaim.Value) : callerId;

            // Fetch the list of juniors in the firm
            var associates = await _userRepository.GetFirmAssociatesAsync(seniorId);

            if (User.IsInRole("Junior"))
            {
                // Juniors also need their boss's info to message them
                var senior = await _userRepository.GetSeniorForJuniorAsync(callerId);
                return Ok(new 
                { 
                    Senior = senior, 
                    Associates = associates // Colleagues
                });
            }

            // Senior only sees associates
            return Ok(new { Associates = associates });
        }

        [HttpPut("firm-bio")]
        public async Task<IActionResult> UpdatedFirmBio([FromBody] string firmBio)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int callerId))
                return Unauthorized();

            await _userRepository.UpdateFirmBioAsync(callerId, firmBio);
            return Ok(new { Message = "Firm bio updated successfully." });
        }   


    }
}
