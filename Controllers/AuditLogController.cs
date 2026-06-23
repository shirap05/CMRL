using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditLogController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/AuditLog
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.ActionTime)
                .ToListAsync();
            return Ok(logs);
        }

        // GET: api/AuditLog/user/5
        [HttpGet("user/{userID}")]
        public async Task<IActionResult> GetByUser(int userID)
        {
            var logs = await _context.AuditLogs
                .Where(l => l.UserID == userID)
                .OrderByDescending(l => l.ActionTime)
                .ToListAsync();
            return Ok(logs);
        }

        // GET: api/AuditLog/recent
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.ActionTime)
                .Take(50)
                .ToListAsync();
            return Ok(logs);
        }

        // GET: api/AuditLog/joindate/{userID}
        // Returns the date the employee was added, derived from the audit log.
        [HttpGet("joindate/{userID}")]
        public async Task<IActionResult> GetJoinDate(int userID)
        {
            // Find the earliest "... added ..." log for this userID
            var log = await _context.AuditLogs
                .Where(l => l.UserID == userID && l.Action.Contains("added"))
                .OrderBy(l => l.ActionTime)
                .FirstOrDefaultAsync();

            if (log == null)
            {
                // Fallback: earliest audit log of any kind for this user
                log = await _context.AuditLogs
                    .Where(l => l.UserID == userID)
                    .OrderBy(l => l.ActionTime)
                    .FirstOrDefaultAsync();
            }

            if (log == null)
                return Ok(new { joinDate = DateTime.UtcNow.ToString("yyyy-MM-dd") });

            // Convert UTC to IST for accurate calendar-day comparison
            var istZone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(log.ActionTime, DateTimeKind.Utc), istZone);

            return Ok(new { joinDate = istTime.ToString("yyyy-MM-dd") });
        }
    }
}