using System.ComponentModel.DataAnnotations;

namespace CaseBridge_Users.DTOs.Firm
{
    public class AddJuniorDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [RegularExpression(@"^[A-Za-z\-\']+(?: [A-Za-z\-\']+)*$", ErrorMessage = "Full Name must contain only letters and single spaces, with no leading or trailing whitespace.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enrollment Number is required.")]
        [RegularExpression(@"^(([A-Z]{2,3}\/\d+\/\d{4})|(AOR-\d{4}-\d{4}))$", ErrorMessage = "Invalid format. Use DL/XXXX/YYYY or AOR-XXXX-YYYY with only capital letters.")]
        public string EnrollmentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required.")]
        [RegularExpression(@"^\S(.*\S)?$", ErrorMessage = "Specialization cannot be empty or contain leading/trailing spaces.")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Temporary Password is required.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[\W_])\S{8,}$", ErrorMessage = "Password must be at least 8 characters long, contain an uppercase letter, lowercase letter, number, and special character, with no spaces.")]
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}
