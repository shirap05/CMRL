using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;
using CMRL.API.Services;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OTPController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public OTPController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // POST: api/OTP/send
        [HttpPost("send")]
        public async Task<IActionResult> SendOTP([FromBody] SendOTPRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserID);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Get email from employee/hr/manager table
            string? email = null;

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserID == request.UserID);
            if (employee != null) email = employee.Email;

            var hr = await _context.HRs
                .FirstOrDefaultAsync(h => h.UserID == request.UserID);
            if (hr != null) email = hr.Email;

            var manager = await _context.Managers
                .FirstOrDefaultAsync(m => m.UserID == request.UserID);
            if (manager != null) email = manager.Email;

            // For admin use user email directly
            if (email == null) email = user.Email;

            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "No email found for this user" });

            // Generate 6 digit OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Save OTP
            var otpRecord = new OTPStore
            {
                UserID = request.UserID,
                OTPCode = otp,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };

            _context.OTPStores.Add(otpRecord);
            await _context.SaveChangesAsync();

            // Send email
            await _emailService.SendOTPAsync(email, otp);

            return Ok(new { message = $"OTP sent to {MaskEmail(email)}" });
        }

        // POST: api/OTP/verify
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPRequest request)
        {
            var otpRecord = await _context.OTPStores
                .Where(o => o.UserID == request.UserID
                    && o.OTPCode == request.OTPCode
                    && !o.IsUsed
                    && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
                return BadRequest(new { message = "Invalid or expired OTP" });

            otpRecord.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "OTP verified successfully", verified = true });
        }

        // POST: api/OTP/change-username
        [HttpPost("change-username")]
        public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameOTPRequest request)
        {
            // Verify OTP first
            var otpRecord = await _context.OTPStores
                .Where(o => o.UserID == request.UserID
                    && o.OTPCode == request.OTPCode
                    && !o.IsUsed
                    && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
                return BadRequest(new { message = "Invalid or expired OTP" });

            // Check username not taken
            var exists = await _context.Users
                .AnyAsync(u => u.Username == request.NewUsername && u.UserID != request.UserID);
            if (exists)
                return BadRequest(new { message = "Username already taken" });

            var user = await _context.Users.FindAsync(request.UserID);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.Username = request.NewUsername;
            otpRecord.IsUsed = true;

            _context.AuditLogs.Add(new AuditLog
            {
                UserID = request.UserID,
                Action = $"Username changed to {request.NewUsername}",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Username changed successfully" });
        }

        // POST: api/OTP/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordOTPRequest request)
        {
            // Verify OTP first
            var otpRecord = await _context.OTPStores
                .Where(o => o.UserID == request.UserID
                    && o.OTPCode == request.OTPCode
                    && !o.IsUsed
                    && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
                return BadRequest(new { message = "Invalid or expired OTP" });

            var user = await _context.Users.FindAsync(request.UserID);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            otpRecord.IsUsed = true;

            _context.AuditLogs.Add(new AuditLog
            {
                UserID = request.UserID,
                Action = "Password changed via OTP",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        private string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2) return email;
            var name = parts[0];
            var masked = name.Length > 3
                ? name.Substring(0, 3) + "***"
                : "***";
            return masked + "@" + parts[1];
        }
    }

    public class SendOTPRequest
    {
        public int UserID { get; set; }
    }

    public class VerifyOTPRequest
    {
        public int UserID { get; set; }
        public string OTPCode { get; set; } = string.Empty;
    }

    public class ChangeUsernameOTPRequest
    {
        public int UserID { get; set; }
        public string OTPCode { get; set; } = string.Empty;
        public string NewUsername { get; set; } = string.Empty;
    }

    public class ChangePasswordOTPRequest
    {
        public int UserID { get; set; }
        public string OTPCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}