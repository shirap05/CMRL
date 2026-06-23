using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;
using CMRL.API.Services;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForgotController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public ForgotController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ── POST: api/Forgot/send-otp-by-email ───────────────────────────────
        [HttpPost("send-otp-by-email")]
        public async Task<IActionResult> SendOTPByEmail([FromBody] ForgotByEmailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest(new { message = "Email address is required." });

                string email = request.Email.ToLower().Trim();
                int? userID = null;
                string? foundEmail = null;

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email.ToLower() == email);
                if (employee != null) { userID = employee.UserID; foundEmail = employee.Email; }

                if (userID == null)
                {
                    var hr = await _context.HRs
                        .FirstOrDefaultAsync(h => h.Email.ToLower() == email);
                    if (hr != null) { userID = hr.UserID; foundEmail = hr.Email; }
                }

                if (userID == null)
                {
                    var manager = await _context.Managers
                        .FirstOrDefaultAsync(m => m.Email.ToLower() == email);
                    if (manager != null) { userID = manager.UserID; foundEmail = manager.Email; }
                }

                if (userID == null)
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == email);
                    if (user != null) { userID = user.UserID; foundEmail = user.Email; }
                }

                if (userID == null)
                    return NotFound(new { message = "No account found with this email address." });

                var otp = new Random().Next(100000, 999999).ToString();

                var existingOTPs = _context.OTPStores
                    .Where(o => o.UserID == userID.Value && !o.IsUsed);
                _context.OTPStores.RemoveRange(existingOTPs);

                _context.OTPStores.Add(new OTPStore
                {
                    UserID    = userID.Value,
                    OTPCode   = otp,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    IsUsed    = false
                });
                await _context.SaveChangesAsync();

                try
                {
                    await _emailService.SendOTPAsync(foundEmail!, otp);
                }
                catch (Exception emailEx)
                {
                    return StatusCode(500, new
                    {
                        message = $"OTP generated but email failed to send. Error: {emailEx.Message}. " +
                                  "Please check your Email settings in appsettings.json."
                    });
                }

                return Ok(new
                {
                    message  = $"OTP sent to {MaskEmail(email)}",
                    userID   = userID.Value
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Server error: {ex.Message}" });
            }
        }

        // ── POST: api/Forgot/reset-password ──────────────────────────────────
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var otpRecord = await _context.OTPStores
                    .Where(o => o.UserID   == request.UserID
                             && o.OTPCode  == request.OTPCode
                             && !o.IsUsed
                             && o.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otpRecord == null)
                    return BadRequest(new { message = "Invalid or expired OTP. Please request a new one." });

                var user = await _context.Users.FindAsync(request.UserID);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                string oldUsernameForLog = user.Username;
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                otpRecord.IsUsed  = true;

                _context.AuditLogs.Add(new AuditLog
                {
                    UserID     = request.UserID,
                    Action     = "Password reset via forgot password",
                    ActionTime = DateTime.UtcNow
                });

                // ── Notify all Admins (same pattern as SettingsController) ────
                var notifiedAdmins = await NotifyAdmins(
                    $"🔔 User '{oldUsernameForLog}' (ID: {request.UserID}) reset their password via Forgot Password.",
                    request.UserID
                );

                await _context.SaveChangesAsync();

                // ── Email all admins ───────────────────────────────────────────
                await EmailAdmins(
                    notifiedAdmins,
                    "Password Reset via Forgot Password — CMRL Workforce",
                    $@"<p>A user's password was reset using the <b>Forgot Password</b> flow:</p>
                       <p><b>User:</b> {oldUsernameForLog} (ID: {request.UserID})</p>
                       <p>If this wasn't expected, please review account security.</p>"
                );

                return Ok(new { message = "Password reset successfully! You can now login." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Server error: {ex.Message}" });
            }
        }

        // ── POST: api/Forgot/get-username ─────────────────────────────────────
        [HttpPost("get-username")]
        public async Task<IActionResult> GetUsername([FromBody] ForgotByEmailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest(new { message = "Email address is required." });

                string email = request.Email.ToLower().Trim();
                int? userID = null;
                string? foundEmail = null;

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email.ToLower() == email);
                if (employee != null) { userID = employee.UserID; foundEmail = employee.Email; }

                if (userID == null)
                {
                    var hr = await _context.HRs
                        .FirstOrDefaultAsync(h => h.Email.ToLower() == email);
                    if (hr != null) { userID = hr.UserID; foundEmail = hr.Email; }
                }

                if (userID == null)
                {
                    var manager = await _context.Managers
                        .FirstOrDefaultAsync(m => m.Email.ToLower() == email);
                    if (manager != null) { userID = manager.UserID; foundEmail = manager.Email; }
                }

                if (userID == null)
                {
                    var adminUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == email);
                    if (adminUser != null) { userID = adminUser.UserID; foundEmail = adminUser.Email; }
                }

                if (userID == null)
                    return NotFound(new { message = "No account found with this email address." });

                var otp = new Random().Next(100000, 999999).ToString();

                var existingOTPs = _context.OTPStores
                    .Where(o => o.UserID == userID.Value && !o.IsUsed);
                _context.OTPStores.RemoveRange(existingOTPs);

                _context.OTPStores.Add(new OTPStore
                {
                    UserID    = userID.Value,
                    OTPCode   = otp,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    IsUsed    = false
                });
                await _context.SaveChangesAsync();

                try
                {
                    await _emailService.SendOTPAsync(foundEmail!, otp);
                }
                catch (Exception emailEx)
                {
                    return StatusCode(500, new
                    {
                        message = $"OTP generated but email failed to send. Error: {emailEx.Message}. " +
                                  "Please check your Email settings in appsettings.json."
                    });
                }

                return Ok(new
                {
                    message = $"OTP sent to {MaskEmail(email)}",
                    userID  = userID.Value
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Server error: {ex.Message}" });
            }
        }

        // ── POST: api/Forgot/verify-and-get-username ──────────────────────────
        [HttpPost("verify-and-get-username")]
        public async Task<IActionResult> VerifyAndGetUsername([FromBody] VerifyUsernameRequest request)
        {
            try
            {
                var otpRecord = await _context.OTPStores
                    .Where(o => o.UserID   == request.UserID
                             && o.OTPCode  == request.OTPCode
                             && !o.IsUsed
                             && o.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otpRecord == null)
                    return BadRequest(new { message = "Invalid or expired OTP. Please request a new one." });

                var user = await _context.Users.FindAsync(request.UserID);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                otpRecord.IsUsed = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "OTP verified successfully.", username = user.Username });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Server error: {ex.Message}" });
            }
        }

        // ── Helper: find admin role and notify all admins ─────────────────────
        private async System.Threading.Tasks.Task<List<User>> NotifyAdmins(string message, int excludeUserID)
        {
            var adminRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole == null) return new List<User>();

            var admins = await _context.Users
                .Where(u => u.RoleID == adminRole.RoleID && u.UserID != excludeUserID)
                .ToListAsync();

            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserID    = admin.UserID,
                    Message   = message,
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return admins;
        }

        // ── Helper: email each admin after the change has been saved ──────────
        private async System.Threading.Tasks.Task EmailAdmins(List<User> admins, string subject, string htmlBody)
        {
            foreach (var admin in admins)
            {
                if (string.IsNullOrWhiteSpace(admin.Email)) continue;
                try
                {
                    await _emailService.SendEmailAsync(admin.Email, subject, htmlBody);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ EMAIL SEND FAILED (ForgotController.NotifyAdmins, AdminUserID={admin.UserID}): {ex.Message}");
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2) return email;
            var name   = parts[0];
            var masked = name.Length > 3 ? name.Substring(0, 3) + "***" : "***";
            return masked + "@" + parts[1];
        }
    }

    public class ForgotByEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public int    UserID      { get; set; }
        public string OTPCode     { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class VerifyUsernameRequest
    {
        public int    UserID  { get; set; }
        public string OTPCode { get; set; } = string.Empty;
    }
}