using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CMRL.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOTPAsync(string toEmail, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("CMRL Workforce", _configuration["Email:From"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "CMRL - Password Reset OTP";

            message.Body = new TextPart("html")
            {
                Text = $@"
                <div style='font-family:Segoe UI;max-width:500px;margin:auto;padding:30px;border:1px solid #ddd;border-radius:12px'>
                    <h2 style='color:#1a237e'>CMRL Workforce Portal</h2>
                    <p>Your OTP for password reset is:</p>
                    <h1 style='color:#1a73e8;letter-spacing:8px'>{otp}</h1>
                    <p>This OTP is valid for <b>10 minutes</b>.</p>
                    <p style='color:#999;font-size:12px'>If you did not request this, please ignore this email.</p>
                </div>"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _configuration["Email:Host"],
                int.Parse(_configuration["Email:Port"]!),
                SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(
                _configuration["Email:Username"],
                _configuration["Email:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        /// <summary>
        /// Generic email sender for all event-based notifications
        /// (task assigned, leave applied/approved, salary calculated, etc.)
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("CMRL Workforce", _configuration["Email:From"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = $@"
                <div style='font-family:Segoe UI;max-width:560px;margin:auto;padding:30px;border:1px solid #ddd;border-radius:12px'>
                    <h2 style='color:#1a237e;margin-bottom:4px'>CMRL Workforce Portal</h2>
                    <hr style='border:none;border-top:1px solid #eee;margin:16px 0' />
                    <div style='font-size:15px;color:#333;line-height:1.6'>{htmlBody}</div>
                    <p style='color:#999;font-size:12px;margin-top:24px'>This is an automated notification from CMRL Workforce Management Portal. Please do not reply to this email.</p>
                </div>"
            };

            using var client2 = new SmtpClient();
            await client2.ConnectAsync(
                _configuration["Email:Host"],
                int.Parse(_configuration["Email:Port"]!),
                SecureSocketOptions.StartTls);
            await client2.AuthenticateAsync(
                _configuration["Email:Username"],
                _configuration["Email:Password"]);
            await client2.SendAsync(message);
            await client2.DisconnectAsync(true);
        }
    }
}