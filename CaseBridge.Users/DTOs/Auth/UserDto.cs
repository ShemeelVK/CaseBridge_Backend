using System.ComponentModel.DataAnnotations;

namespace CaseBridge_Users.DTOs.Auth
{
    public class RegisterClientDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        // Defaulting to "Individual" keeps it simple for now
        public string ClientType { get; set; } = "Individual";
    }

    // Extension: Specific to Lawyers
    public class RegisterLawyerDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string EnrollmentNumber { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;

        public string? FirmBio { get; set; }
    }
    public class UserDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; }
    }
    public class TokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
        public string UserType { get; set; } = "Client";
        public int? SeniorLawyerId { get; set; } // If a Junior registers via Google
    }
    public class ForgotPasswordRequest 
    {
        public string Email { get; set; } = string.Empty; 
    }
    public class ResetPasswordDto
    {
        public required string Email { get; set; }
        public required string Token { get; set; }
        public required string NewPassword { get; set; }
    }
}
