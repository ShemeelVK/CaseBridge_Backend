using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace CaseBridge_Users.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var portStr = _config["EmailSettings:Port"];
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var password = _config["EmailSettings:SenderPassword"];

            // Log the email content to console for development testing (Enterprise Standard)
            _logger.LogInformation("=================================================");
            _logger.LogInformation("OUTGOING EMAIL TO: {Email}", toEmail);
            _logger.LogInformation("SUBJECT: {Subject}", subject);
            _logger.LogInformation("BODY: {Body}", body);
            _logger.LogInformation("=================================================");

            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(portStr))
            {
                _logger.LogWarning("SMTP Settings not configured. Email not sent, only logged to console.");
                return;
            }

            try
            {
                using var client = new SmtpClient(smtpServer, int.Parse(portStr))
                {
                    Credentials = new NetworkCredential(senderEmail, password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage(senderEmail!, toEmail, subject, body) { IsBodyHtml = true };
                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SMTP.");
                // We don't throw here in dev so the app doesn't crash if SMTP is missing
            }
        }

        public async Task SendVerificationEmailAsync(string toEmail, string name, string token)
        {
            var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "http://localhost:5173";
            var link = $"{baseUrl}/verify-email?token={token}&email={toEmail}";
            
            _logger.LogInformation(">>> DEBUG VERIFICATION LINK: {Link}", link);
            
            var subject = "Verify Your CaseBridge Account";
            var body = $@"
                <div style='font-family: Arial, sans-serif; border: 1px solid #eee; padding: 20px; max-width: 600px;'>
                    <h2 style='color: #1a237e;'>Welcome to CaseBridge!</h2>
                    <p>Hello <strong>{name}</strong>,</p>
                    <p>Please click the button below to verify your email address and activate your CaseBridge account.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{link}' style='background: #b8860b; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Verify Account</a>
                    </div>
                    <p style='font-size: 12px; color: #666;'>If you did not create this account, please ignore this email.</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendResetPasswordEmailAsync(string toEmail, string userName, string resetToken, int expiryMinutes)
        {
            var baseUrl = _config["FrontendSettings:BaseUrl"] ?? "http://localhost:5173";
            var resetLink = $"{baseUrl}/reset-password?token={resetToken}&email={toEmail}";
            
            _logger.LogInformation(">>> DEBUG RESET LINK: {Link}", resetLink);
            
            var subject = "Action Required: Reset Your CaseBridge Password";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px;'>
                    <h2 style='color: #1a237e;'>CaseBridge Legal Portal</h2>
                    <p>Hello <strong>{userName}</strong>,</p>
                    <p>We received a request to reset the password for your CaseBridge account. If you didn't make this request, you can safely ignore this email.</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' 
                           style='background-color: #b8860b; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                           Reset Password
                        </a>
                    </div>

                    <p style='font-size: 14px; color: #555;'>
                        <strong>Note:</strong> This link is valid for <strong>{expiryMinutes} minutes</strong> only. 
                    </p>
                    <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='font-size: 12px; color: #888;'>Sent by CaseBridge Security Team</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
