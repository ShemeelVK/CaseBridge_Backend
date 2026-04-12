namespace CaseBridge_Users.Models
{
    public class UserSecurity
    {
        public int UserId { get; set; }
        public string? PasswordHash { get; set; }
        public bool IsEmailVerified { get; set; }
        public string? VerificationToken { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool IsLocked { get; set; }

        // Session Management
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
