using CaseBridge_Users.DTOs.Auth;
using CaseBridge_Users.Models;
using CaseBridge_Users.Repositories;
using CaseBridge_Users.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace CaseBridge_Users.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public UserController(
            UserRepository userRepository, 
            TokenService tokenService, 
            IConfiguration configuration,
            EmailService emailService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _configuration = configuration;
            _emailService = emailService;
        }

        #region Registration

        [HttpPost("register/client")]
        public async Task<IActionResult> RegisterClient([FromBody] RegisterClientDto dto)
        {
            // 1. Map to the common User object
            var user = new User
            {
                Email = dto.Email,
                FullName = dto.FullName,
                UserType = "Client"
            };

            // 2. Map to the specific ClientProfile object
            var profile = new ClientProfile
            {
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                ClientType = dto.ClientType
            };

            var verificationToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

            // 3. Pass both to the Repository
            var success = await _userRepository.RegisterClientAsync(user, profile, dto.Password, verificationToken);

            if (!success) return BadRequest("Email might already be in use.");

            await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, verificationToken);

            return Ok(new { Message = "Client account created. Please check your email for verification." });
        }

        [HttpPost("register/lawyer")]
        public async Task<IActionResult> RegisterLawyer([FromBody] RegisterLawyerDto dto)
        {
            var user = new User
            {
                Email = dto.Email,
                FullName = dto.FullName,
                UserType = "Lawyer"
            };

            var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            var profile = new LawyerProfile
            {
                EnrollmentNumber = dto.EnrollmentNumber,
                Specialization = dto.Specialization,
                SeniorLawyerId=null,
                FirmBio=dto.FirmBio,
            };

            var success = await _userRepository.RegisterLawyerAsync(user, profile, dto.Password,verificationToken);
            if (!success) return BadRequest("Registration failed.");

            await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, verificationToken);

            return Ok(new { Message = "Registration successful! Check your inbox." });
        }

        #endregion

        #region Authentication Core (Login & Refresh)

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDTO dto)
        {
           
            var (user, security) = await _userRepository.GetUserWithSecurityAsync(dto.Email.ToLower().Trim());

            if (user == null || security == null)
                return Unauthorized("Invalid credentials.");

            if (security.LockoutEnd.HasValue && security.LockoutEnd > DateTime.Now)
                return StatusCode(403, $"Account locked. Try again after {security.LockoutEnd}");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, security.PasswordHash))
            {
                security.FailedLoginAttempts++;
                if (security.FailedLoginAttempts >= 5) 
                    security.LockoutEnd = DateTime.Now.AddMinutes(10);
                
                await _userRepository.UpdateSecurityStatusAsync(security);
                return Unauthorized("Invalid credentials.");
            }

            if (!security.IsEmailVerified)
                return StatusCode(403, "Please verify your email address first.");

            // 5. Fetch Lawyer Profile (to get the SeniorId for the JWT)
            // Clients won't have a profile, so this will be null for them.
            var (_, profile) = await _userRepository.GetUserAndProfileAsync(user.Id);

            var accessToken = _tokenService.CreateToken(user, profile);
            var refreshToken = _tokenService.GenerateRefreshToken();

            security.FailedLoginAttempts= 0;
            await _userRepository.UpdateRefreshTokenAsync(user.Id, refreshToken, DateTime.Now.AddDays(7));
            await _userRepository.UpdateSecurityStatusAsync(security);

            return Ok(new { 
                AccessToken = accessToken, 
                RefreshToken = refreshToken,
                UserType = user.UserType,
                FullName = user.FullName,
                Email = user.Email,
                Id = user.Id
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> Refresh([FromBody] TokenRequestDto dto)
        {
            var (user, security) = await _userRepository.GetUserByRefreshTokenAsync(dto.RefreshToken);

            if (user == null || security == null || security.RefreshTokenExpiryTime <= DateTime.Now)
                return Unauthorized("Session expired. Please login again.");

            // Fetch profile here too to ensure the new access token keeps the SeniorId
            var (_, profile) = await _userRepository.GetUserAndProfileAsync(user.Id);

            var newAccessToken = _tokenService.CreateToken(user,profile);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            await _userRepository.UpdateRefreshTokenAsync(user.Id, newRefreshToken, DateTime.Now.AddDays(7));

            return Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
        }

        #endregion

        #region OAuth & Security Tools

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { _configuration["Google:ClientId"]! }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);

                //initial lookup using email
                var email = payload.Email?.ToLower().Trim() ?? string.Empty;
                var (user, security) = await _userRepository.GetUserWithSecurityAsync(email);

                if (user == null)
                {
                    if (dto.LoginOnly)
                    {
                        return NotFound("User not found. Please complete registration.");
                    }
                    
                    user = new User
                    {
                        Email = payload.Email,
                        FullName = payload.Name,
                        UserType = dto.UserType, 
                        GoogleId = payload.Subject
                    };

                    if (dto.UserType == "Lawyer")
                    {
                        var newProfile = new LawyerProfile
                        {
                            EnrollmentNumber = dto.EnrollmentNumber,
                            Specialization = dto.Specialization ?? "To be specified",
                            FirmBio = dto.FirmBio,
                            SeniorLawyerId = dto.SeniorLawyerId
                        };
                        // Register using the 3-table transaction
                        await _userRepository.RegisterLawyerAsync(user, newProfile, string.Empty, string.Empty);
                    }
                    else
                    {
                        var newClientProfile = new ClientProfile
                        {
                            PhoneNumber = dto.PhoneNumber,
                            Address = dto.Address,
                            ClientType = dto.ClientType ?? "Individual"
                        };

                        // FIX: Register using the 3-table transaction (Client) - Passing all 4 arguments
                        await _userRepository.RegisterClientAsync(user, newClientProfile, string.Empty, string.Empty);
                    }

                    (_, security) = await _userRepository.GetUserWithSecurityAsync(email);
                    security!.IsEmailVerified = true;
                    await _userRepository.UpdateSecurityStatusAsync(security);
                }

                    else if (string.IsNullOrEmpty(user.GoogleId))
                    {
                        user.GoogleId = payload.Subject;
                        security!.IsEmailVerified = true;
                        await _userRepository.UpdateUserAsync(user);
                        await _userRepository.UpdateSecurityStatusAsync(security);
                    }

                // Finalizing the session

                var (_, lawyerProfile) = await _userRepository.GetUserAndProfileAsync(user.Id);

                var token = _tokenService.CreateToken(user,lawyerProfile);
                var refreshToken = _tokenService.GenerateRefreshToken();

                await _userRepository.UpdateRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));

                return Ok(new
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    UserType = user.UserType,
                    FullName = user.FullName,
                    Email = user.Email,
                    Id = user.Id
                });
            }
            catch (InvalidJwtException) { return BadRequest("Invalid Google token."); }
            catch (Exception ex) { return StatusCode(500, $"Google Auth Failed: {ex.Message}"); }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var (user, security) = await _userRepository.GetUserWithSecurityAsync(request.Email);
            if (user == null || security == null) return Ok("If an account exists, a link has been sent.");

            var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            security.PasswordResetToken = resetToken;
            security.ResetTokenExpiry = DateTime.Now.AddMinutes(15);

            await _userRepository.UpdateSecurityStatusAsync(security);

            await _emailService.SendResetPasswordEmailAsync(user.Email, user.FullName, resetToken, 15);

            return Ok("Reset link generated in database.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
        
            var (user, security) = await _userRepository.GetUserWithSecurityAsync(dto.Email);

            if (user == null || security == null)
                return BadRequest("Invalid request.");


            if (security.PasswordResetToken != dto.Token || security.ResetTokenExpiry < DateTime.Now)
            {
                return BadRequest("Invalid or expired reset token.");
            }


            security.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

           
            security.PasswordResetToken = null;
            security.ResetTokenExpiry = null;
            security.FailedLoginAttempts = 0;
            security.LockoutEnd = null;

            await _userRepository.UpdateSecurityStatusAsync(security);

            return Ok(new { Message = "Password has been reset successfully. You can now log in." });
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail(string email, string token)
        {
            var (user, security) = await _userRepository.GetUserWithSecurityAsync(email);

            if (security == null || security.VerificationToken != token)
                return BadRequest("Invalid or expired verification link.");

            security.IsEmailVerified = true;
            security.VerificationToken = null;
            
            await _userRepository.UpdateSecurityStatusAsync(security);
            return Ok("Email verified! You can now log in.");
        }

        [Authorize]
        [HttpGet("Profile")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value,out int userId)) return Unauthorized();

            var (fetchedUser, profile) = await _userRepository.GetUserAndProfileAsync(userId);
            if (fetchedUser == null) return NotFound();

            return Ok(new
            {
                Id = fetchedUser.Id,
                Email = fetchedUser.Email,
                FullName = fetchedUser.FullName,
                Role = fetchedUser.UserType,
                FirmBio = profile?.FirmBio,
                SeniorLawyerId = profile?.SeniorLawyerId
            });
        }

        #endregion
    }
}