using System.ComponentModel.DataAnnotations;

namespace CaseBridge_Users.DTOs.Auth
{
    public class RegisterClientDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[\W_])\S{8,}$", ErrorMessage = "Password must be at least 8 characters long, contain an uppercase letter, lowercase letter, number, and special character, with no spaces.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [RegularExpression(@"^[A-Za-z\-\']+(?: [A-Za-z\-\']+)*$", ErrorMessage = "Full Name must contain only letters and single spaces, with no leading or trailing whitespace.")]
        public string FullName { get; set; } = string.Empty;

        [RegularExpression(@"^\+?\d{10}$", ErrorMessage = "Phone number must be 10 digits, optionally starting with a '+'.")] 
        public string? PhoneNumber { get; set; }
        [RegularExpression(@"^\S(.*?\S)?$", ErrorMessage = "Address cannot be empty or contain only whitespace.")]
        public string? Address { get; set; } 

        // Defaulting to "Individual" keeps it simple for now
        public string ClientType { get; set; } = "Individual";
    }

    // Extension: Specific to Lawyers
    public class RegisterLawyerDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[\W_])\S{8,}$", ErrorMessage = "Password must be at least 8 characters long, contain an uppercase letter, lowercase letter, number, and special character, with no spaces.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [RegularExpression(@"^[A-Za-z\-\']+(?: [A-Za-z\-\']+)*$", ErrorMessage = "Full Name must contain only letters and single spaces, with no leading or trailing whitespace.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enrollment Number is required.")]
        [RegularExpression(@"^(([A-Z]{2,3}\/\d+\/\d{4})|(AOR-\d{4}-\d{4}))$", ErrorMessage = "Invalid format. Use DL/XXXX/YYYY or AOR-XXXX-YYYY with only capital letters.")]
        public string EnrollmentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required.")]
        [RegularExpression(@"^\S(.*\S)?$", ErrorMessage = "Specialization cannot be empty or contain leading/trailing spaces.")]
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
        
        // Additional optional fields for two-step registration
        public string? EnrollmentNumber { get; set; }
        public string? Specialization { get; set; }
        public string? FirmBio { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? ClientType { get; set; }
        
        public bool LoginOnly { get; set; } = false;
    }
    public class ForgotPasswordRequest 
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty; 
    }
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Token is required.")]
        public required string Token { get; set; }

        [Required(ErrorMessage = "New Password is required.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[\W_])\S{8,}$", ErrorMessage = "Password must be at least 8 characters long, contain an uppercase letter, lowercase letter, number, and special character, with no spaces.")]
        public required string NewPassword { get; set; }
    }
}
