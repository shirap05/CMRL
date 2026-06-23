using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;
using CMRL.API.Services;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public SettingsController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet("{userID}")]
        public async Task<IActionResult> GetSettings(int userID)
        {
            var settings = await _context.Settings
                .FirstOrDefaultAsync(s => s.UserID == userID);
            if (settings == null)
            {
                settings = new Setting { UserID = userID, Theme = "Light" };
                _context.Settings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return Ok(settings);
        }

        [HttpPut("theme")]
        public async Task<IActionResult> UpdateTheme([FromBody] UpdateThemeRequest request)
        {
            var settings = await _context.Settings
                .FirstOrDefaultAsync(s => s.UserID == request.UserID);
            if (settings == null)
            {
                _context.Settings.Add(new Setting { UserID = request.UserID, Theme = request.Theme });
            }
            else
            {
                settings.Theme = request.Theme;
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Theme updated successfully" });
        }

        [HttpPut("username")]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserID);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var exists = await _context.Users
                .AnyAsync(u => u.Username == request.NewUsername && u.UserID != request.UserID);
            if (exists)
                return BadRequest(new { message = "Username already taken" });

            string oldUsername = user.Username;
            user.Username = request.NewUsername;

            // ── Notify all Admins (excluding the user themselves if they are admin) ──
            var notifiedAdmins = await NotifyAdmins(
                $"🔔 User '{oldUsername}' (ID: {request.UserID}) changed their username to '{request.NewUsername}'.",
                request.UserID
            );

            _context.AuditLogs.Add(new AuditLog
            {
                UserID     = request.UserID,
                Action     = $"Username changed from '{oldUsername}' to '{request.NewUsername}'",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to all admins ───────────────────────────────────────
            await EmailAdmins(
                notifiedAdmins,
                "Username Changed — CMRL Workforce",
                $@"<p>A user's username has been changed:</p>
                   <p><b>User ID:</b> {request.UserID}<br/>
                      <b>Old Username:</b> {oldUsername}<br/>
                      <b>New Username:</b> {request.NewUsername}</p>"
            );

            return Ok(new { message = "Username updated successfully" });
        }

        [HttpPut("password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserID);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Accept either OldPassword or CurrentPassword field
            var oldPwd = !string.IsNullOrEmpty(request.OldPassword)
                ? request.OldPassword
                : request.CurrentPassword;

            if (!BCrypt.Net.BCrypt.Verify(oldPwd, user.PasswordHash))
                return BadRequest(new { message = "Current password is incorrect" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // ── Notify all Admins ─────────────────────────────────────────────
            var notifiedAdmins = await NotifyAdmins(
                $"🔔 User '{user.Username}' (ID: {request.UserID}) changed their password.",
                request.UserID
            );

            _context.AuditLogs.Add(new AuditLog
            {
                UserID     = request.UserID,
                Action     = "Password changed via Settings",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to all admins ───────────────────────────────────────
            await EmailAdmins(
                notifiedAdmins,
                "Password Changed — CMRL Workforce",
                $@"<p>A user's password has been changed:</p>
                   <p><b>User:</b> {user.Username} (ID: {request.UserID})</p>
                   <p>If this wasn't expected, please review account security.</p>"
            );

            return Ok(new { message = "Password updated successfully" });
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
                    Console.WriteLine($"⚠️ EMAIL SEND FAILED (NotifyAdmins, AdminUserID={admin.UserID}): {ex.Message}");
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }

    public class UpdateThemeRequest
    {
        public int    UserID { get; set; }
        public string Theme  { get; set; } = "Light";
    }

    public class UpdateUsernameRequest
    {
        public int    UserID          { get; set; }
        public string NewUsername     { get; set; } = string.Empty;
        public string CurrentUsername { get; set; } = string.Empty; // optional, for validation
    }

    public class UpdatePasswordRequest
    {
        public int    UserID          { get; set; }
        public string OldPassword     { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty; // alias for OldPassword
        public string NewPassword     { get; set; } = string.Empty;
    }
}