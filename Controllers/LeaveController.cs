using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;
using CMRL.API.Services;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public LeaveController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var leaves = await _context.Leaves
                .OrderByDescending(l => l.AppliedAt)
                .ToListAsync();
            return Ok(leaves);
        }

        [HttpGet("user/{userID}")]
        public async Task<IActionResult> GetByUser(int userID)
        {
            var leaves = await _context.Leaves
                .Where(l => l.UserID == userID)
                .OrderByDescending(l => l.AppliedAt)
                .ToListAsync();
            return Ok(leaves);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var leaves = await _context.Leaves
                .Where(l => l.Status == "Pending")
                .OrderByDescending(l => l.AppliedAt)
                .ToListAsync();
            return Ok(leaves);
        }

        [HttpPost]
        public async Task<IActionResult> Apply([FromBody] LeaveApplyRequest request)
        {
            var leave = new Leave
            {
                UserID      = request.UserID,
                FromDate    = DateOnly.Parse(request.FromDate),
                ToDate      = DateOnly.Parse(request.ToDate),
                Description = request.Description,
                Status      = "Pending",
                AppliedAt   = DateTime.UtcNow
            };
            _context.Leaves.Add(leave);

            // Get employee details
            var leaveEmp = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserID == request.UserID);
            string empName = leaveEmp?.FullName ?? $"User {request.UserID}";

            // Notify Manager only (not HR)
            if (leaveEmp?.ManagerID != null)
            {
                var leaveManager = await _context.Managers
                    .FirstOrDefaultAsync(m => m.ManagerID == leaveEmp.ManagerID);
                if (leaveManager != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserID    = leaveManager.UserID,
                        Message   = $"🏖️ {empName} has requested leave from {request.FromDate} to {request.ToDate}. Please review.",
                        IsRead    = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Notify the employee that leave request was submitted
            _context.Notifications.Add(new Notification
            {
                UserID    = request.UserID,
                Message   = $"🏖️ Your leave request from {request.FromDate} to {request.ToDate} has been submitted successfully.",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserID     = request.UserID,
                Action     = $"Leave applied from {request.FromDate} to {request.ToDate}",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to Manager ─────────────────────────────────────────
            if (leaveEmp?.ManagerID != null)
            {
                var leaveManager = await _context.Managers
                    .FirstOrDefaultAsync(m => m.ManagerID == leaveEmp.ManagerID);
                if (leaveManager != null && !string.IsNullOrWhiteSpace(leaveManager.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            leaveManager.Email,
                            "New Leave Request — CMRL Workforce",
                            $@"<p>Hi {leaveManager.FullName},</p>
                               <p><b>{empName}</b> has requested leave:</p>
                               <p><b>From:</b> {request.FromDate}<br/>
                                  <b>To:</b> {request.ToDate}<br/>
                                  <b>Reason:</b> {request.Description}</p>
                               <p>Please review and approve/reject this request in the CMRL Workforce Portal.</p>"
                        );
                    }
                    catch { /* Email failure should never block leave application */ }
                }
            }

            return Ok(new { message = "Leave applied successfully", leaveID = leave.LeaveID });
        }

        [HttpPut("approve/{id}")]
        public async Task<IActionResult> Approve(int id, [FromBody] LeaveActionRequest request)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null)
                return NotFound(new { message = "Leave not found" });

            leave.Status     = "Approved";
            leave.ApprovedBy = request.ApprovedBy;

            // Notify employee
            _context.Notifications.Add(new Notification
            {
                UserID    = leave.UserID,
                Message   = $"✅ Your leave request ({leave.FromDate} to {leave.ToDate}) has been approved.",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserID     = request.ApprovedBy,
                Action     = $"Leave {id} approved",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to employee ────────────────────────────────────────
            var approvedEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserID == leave.UserID);
            if (approvedEmp != null && !string.IsNullOrWhiteSpace(approvedEmp.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        approvedEmp.Email,
                        "Leave Request Approved — CMRL Workforce",
                        $@"<p>Hi {approvedEmp.FullName},</p>
                           <p>Your leave request has been <b style='color:#2e7d32'>approved</b>.</p>
                           <p><b>From:</b> {leave.FromDate}<br/>
                              <b>To:</b> {leave.ToDate}</p>
                           <p>Enjoy your time off!</p>"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ EMAIL SEND FAILED (Approve, UserID={leave.UserID}, Email={approvedEmp.Email}): {ex.Message}");
                    Console.WriteLine(ex.ToString());
                }
            }

            return Ok(new { message = "Leave approved successfully" });
        }

        [HttpPut("reject/{id}")]
        public async Task<IActionResult> Reject(int id, [FromBody] LeaveActionRequest request)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null)
                return NotFound(new { message = "Leave not found" });

            leave.Status     = "Rejected";
            leave.ApprovedBy = request.ApprovedBy;

            // Notify employee
            _context.Notifications.Add(new Notification
            {
                UserID    = leave.UserID,
                Message   = $"❌ Your leave request ({leave.FromDate} to {leave.ToDate}) has been rejected.",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserID     = request.ApprovedBy,
                Action     = $"Leave {id} rejected",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to employee ────────────────────────────────────────
            var rejectedEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserID == leave.UserID);
            if (rejectedEmp != null && !string.IsNullOrWhiteSpace(rejectedEmp.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        rejectedEmp.Email,
                        "Leave Request Rejected — CMRL Workforce",
                        $@"<p>Hi {rejectedEmp.FullName},</p>
                           <p>Your leave request has been <b style='color:#c62828'>rejected</b>.</p>
                           <p><b>From:</b> {leave.FromDate}<br/>
                              <b>To:</b> {leave.ToDate}</p>
                           <p>Please contact your manager for more details.</p>"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ EMAIL SEND FAILED (Reject, UserID={leave.UserID}, Email={rejectedEmp.Email}): {ex.Message}");
                    Console.WriteLine(ex.ToString());
                }
            }

            return Ok(new { message = "Leave rejected successfully" });
        }

        [HttpGet("balance/{userID}")]
public async Task<IActionResult> GetBalance(int userID)
{
    var currentMonth = DateTime.UtcNow.Month;
    var currentYear  = DateTime.UtcNow.Year;

    var leaves = await _context.Leaves
        .Where(l => l.UserID == userID
            && l.Status == "Approved"
            && l.FromDate.Month == currentMonth
            && l.FromDate.Year  == currentYear)
        .ToListAsync();

    var usedDays = leaves.Sum(l => (l.ToDate.DayNumber - l.FromDate.DayNumber + 1));

    var balance = Math.Max(0, 2 - usedDays);
    return Ok(new { balance, used = usedDays });
}}

    public class LeaveApplyRequest
    {
        public int    UserID      { get; set; }
        public string FromDate    { get; set; } = string.Empty;
        public string ToDate      { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class LeaveActionRequest
    {
        public int ApprovedBy { get; set; }
    }
}