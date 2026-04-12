namespace CaseBridge_Users.Models
{
    public class ClientProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string ClientType { get; set; } = "Individual";
    }
}