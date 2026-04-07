using MailKit;
using System.Net;
using System.Net.Mail;

namespace CaseBridge_Users.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:Port"]!);
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var password = _config["EmailSettings:SenderPassword"];

            using var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage(senderEmail!, toEmail, subject, body) { IsBodyHtml = true };
            await client.SendMailAsync(mailMessage);
        }

        public async Task SendVerificationEmailAsync(string toEmail, string name, string link)
        {
            var subject = "Verify Your CaseBridge Account";
            var body = $@"
        <div style='font-family: Arial, sans-serif; border: 1px solid #eee; padding: 20px;'>
            <h2>Welcome to CaseBridge!</h2>
            <p>Hello {name},</p>
            <p>Please click the button below to verify your email address and activate your account.</p>
            <a href='{link}' style='background: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Account</a>
            <p>If you did not create this account, please ignore this email.</p>
        </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendResetPasswordEmailAsync(string toEmail, string userName, string resetLink, int expiryMinutes)
        {
            var subject = "Action Required: Reset Your CaseBridge Password";

            // This is the professional HTML template for the legal portal
            var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px;'>
            <h2 style='color: #2c3e50;'>CaseBridge Legal Portal</h2>
            <p>Hello <strong>{userName}</strong>,</p>
            <p>We received a request to reset the password for your CaseBridge account. If you didn't make this request, you can safely ignore this email.</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{resetLink}' 
                   style='background-color: #1a73e8; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                   Reset Password
                </a>
            </div>

            <p style='font-size: 14px; color: #555;'>
                <strong>Note:</strong> This link is valid for <strong>{expiryMinutes} minutes</strong> only. 
            </p>
            <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
            <p style='font-size: 12px; color: #888;'>Sent by CaseBridge Dev Team</p>
        </div>";

            // Call your existing generic SendEmailAsync method
            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
