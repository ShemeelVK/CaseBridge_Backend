using System.ComponentModel.DataAnnotations;

namespace CaseBridge_Users.DTOs.Firm
{
    public class AddJuniorDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string EnrollmentNumber { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}
