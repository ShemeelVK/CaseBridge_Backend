using System.ComponentModel.DataAnnotations;

namespace CaseBridge_Users.Dtos
{
    public class RegisterClientDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    // Extension: Specific to Lawyers
    public class RegisterLawyerDto : RegisterClientDto
    {
        [Required]
        public string EnrollmentNumber { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;

        // For Junior Lawyers: Link them to their boss
        public int? SeniorLawyerId { get; set; }

        // For Senior Lawyers / Firms: Their professional profile
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
