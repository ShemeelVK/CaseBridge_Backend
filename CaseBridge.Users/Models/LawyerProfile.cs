namespace CaseBridge_Users.Models
{
    public class LawyerProfile
    {
        public int Id { get; set; } // Changed from Guid
        public int UserId { get; set; } // Changed from Guid
        public int? SeniorLawyerId { get; set; }
        public string EnrollmentNumber { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string FirmBio {  get; set; } = string.Empty;
    }
}
