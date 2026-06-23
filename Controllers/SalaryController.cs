using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;
using CMRL.API.Services;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalaryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public SalaryController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var salaries = await _context.Salaries
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .ToListAsync();
            return Ok(salaries);
        }

        [HttpGet("user/{userID}")]
        public async Task<IActionResult> GetByUser(int userID)
        {
            var salaries = await _context.Salaries
                .Where(s => s.UserID == userID)
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .ToListAsync();
            return Ok(salaries);
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] SalaryCalculateRequest request)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserID == request.UserID);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            // ── Get employee join date from audit log ─────────────────────────
            var joinLog = await _context.AuditLogs
                .Where(l => l.UserID == request.UserID && l.Action.Contains("added"))
                .OrderBy(l => l.ActionTime)
                .FirstOrDefaultAsync();

            var istZone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
            var joinDate = joinLog != null
                ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(joinLog.ActionTime.ToUniversalTime(), istZone))
                : new DateOnly(request.Year, request.Month, 1);

            // ── Count working days in month (Mon–Fri) from join date ──────────
            var firstDay        = new DateOnly(request.Year, request.Month, 1);
            var lastDayOfMonth  = new DateOnly(request.Year, request.Month,
                                    DateTime.DaysInMonth(request.Year, request.Month));

            // Don't count days that haven't happened yet. If calculating for the
            // current month, stop at today (in IST); past months still use the
            // full month, since those days are all in the past anyway.
            var todayIst = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone));
            var lastDay  = todayIst < lastDayOfMonth ? todayIst : lastDayOfMonth;

            var effectiveFrom = joinDate > firstDay ? joinDate : firstDay;

            int totalWorkingDays = 0;
            for (var d = effectiveFrom; d <= lastDay; d = d.AddDays(1))
            {
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    totalWorkingDays++;
            }

            // ── Count approved leaves this month ──────────────────────────────
            var leaves = await _context.Leaves
                .Where(l => l.UserID   == request.UserID
                         && l.Status  == "Approved"
                         && l.FromDate.Month == request.Month
                         && l.FromDate.Year  == request.Year)
                .ToListAsync();

            var leaveCount  = leaves.Sum(l => (l.ToDate.DayNumber - l.FromDate.DayNumber + 1));
            var extraLeaves = Math.Max(0, leaveCount - 2);  // 2 free leaves/month

            // ── Count absent days this month (present days vs working days) ───
            var attendances = await _context.Attendances
                .Where(a => a.UserID == request.UserID
                         && a.AttendanceDate.Month == request.Month
                         && a.AttendanceDate.Year  == request.Year
                         && a.AttendanceDate       >= effectiveFrom)
                .ToListAsync();

            var presentDays = attendances.Count(a => a.Status == "Present");
            var leaveDays   = leaveCount;
            var absentDays  = Math.Max(0, totalWorkingDays - presentDays - leaveDays);

            // ── Count late entries this month ─────────────────────────────────
            var lateCount = attendances.Count(a => a.IsLate);

            // ── Calculate deductions ──────────────────────────────────────────
            // ₹500 per extra leave day (beyond 2 free), ₹500 per absent day, ₹200 per late entry
            var leaveDeduction  = extraLeaves * 500m;
            var absentDeduction = absentDays  * 500m;
            var lateDeduction   = lateCount   * 200m;
            var totalDeduction  = leaveDeduction + absentDeduction + lateDeduction;
            var finalSalary     = employee.BasicSalary - totalDeduction;
            if (finalSalary < 0) finalSalary = 0;

            var monthName = new DateTime(request.Year, request.Month, 1).ToString("MMMM yyyy");

            // ── Save or update salary record ──────────────────────────────────
            var existing = await _context.Salaries
                .FirstOrDefaultAsync(s => s.UserID == request.UserID
                                       && s.Month  == request.Month
                                       && s.Year   == request.Year);

            if (existing != null)
            {
                existing.BasicSalary      = employee.BasicSalary;
                existing.LeaveCount       = leaveCount;
                existing.DeductionAmount  = totalDeduction;
                existing.FinalSalary      = finalSalary;
                existing.IsApproved       = false;
                existing.AbsentDays       = absentDays;
                existing.LateCount        = lateCount;
                existing.LeaveDeduction   = leaveDeduction;
                existing.AbsentDeduction  = absentDeduction;
                existing.LateDeduction    = lateDeduction;
            }
            else
            {
                _context.Salaries.Add(new Salary
                {
                    UserID           = request.UserID,
                    Month            = request.Month,
                    Year             = request.Year,
                    BasicSalary      = employee.BasicSalary,
                    LeaveCount       = leaveCount,
                    DeductionAmount  = totalDeduction,
                    FinalSalary      = finalSalary,
                    IsApproved       = false,
                    AbsentDays       = absentDays,
                    LateCount        = lateCount,
                    LeaveDeduction   = leaveDeduction,
                    AbsentDeduction  = absentDeduction,
                    LateDeduction    = lateDeduction
                });
            }

            // ── Notify employee ───────────────────────────────────────────────
            _context.Notifications.Add(new Notification
            {
                UserID    = request.UserID,
                Message   = $"💰 Your salary for {monthName} has been calculated. " +
                            $"Basic: ₹{employee.BasicSalary} | " +
                            $"Leave deduction: ₹{leaveDeduction} ({extraLeaves} extra days) | " +
                            $"Absent deduction: ₹{absentDeduction} ({absentDays} days) | " +
                            $"Late deduction: ₹{lateDeduction} ({lateCount} entries) | " +
                            $"Final: ₹{finalSalary}. Pending approval.",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserID     = request.UserID,
                Action     = $"Salary calculated for {monthName}: Leave ₹{leaveDeduction}, Absent ₹{absentDeduction}, Late ₹{lateDeduction}, Final ₹{finalSalary}",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to employee ────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(employee.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        employee.Email,
                        $"Salary Calculated for {monthName} — CMRL Workforce",
                        $@"<p>Hi {employee.FullName},</p>
                           <p>Your salary for <b>{monthName}</b> has been calculated:</p>
                           <table style='width:100%;border-collapse:collapse;margin-top:10px'>
                             <tr><td style='padding:6px 0;color:#666'>Basic Salary</td><td style='padding:6px 0;text-align:right'>₹{employee.BasicSalary}</td></tr>
                             <tr><td style='padding:6px 0;color:#666'>Leave Deduction ({extraLeaves} extra days)</td><td style='padding:6px 0;text-align:right;color:#c62828'>−₹{leaveDeduction}</td></tr>
                             <tr><td style='padding:6px 0;color:#666'>Absent Deduction ({absentDays} days)</td><td style='padding:6px 0;text-align:right;color:#c62828'>−₹{absentDeduction}</td></tr>
                             <tr><td style='padding:6px 0;color:#666'>Late Deduction ({lateCount} entries)</td><td style='padding:6px 0;text-align:right;color:#c62828'>−₹{lateDeduction}</td></tr>
                             <tr style='border-top:2px solid #eee'><td style='padding:10px 0;font-weight:bold'>Final Salary</td><td style='padding:10px 0;text-align:right;font-weight:bold;color:#2e7d32'>₹{finalSalary}</td></tr>
                           </table>
                           <p style='margin-top:12px'>This is currently <b>pending approval</b> from HR.</p>"
                    );
                }
                catch { /* Email failure should never block salary calculation */ }
            }

            return Ok(new
            {
                message          = "Salary calculated successfully",
                basicSalary      = employee.BasicSalary,
                totalWorkingDays,
                presentDays,
                absentDays,
                leaveCount,
                extraLeaves,
                lateCount,
                leaveDeduction,
                absentDeduction,
                lateDeduction,
                deductionAmount  = totalDeduction,
                finalSalary
            });
        }

        [HttpPut("approve/{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            var salary = await _context.Salaries.FindAsync(id);
            if (salary == null)
                return NotFound(new { message = "Salary record not found" });

            salary.IsApproved = true;

            var monthName = new DateTime(salary.Year, salary.Month, 1).ToString("MMMM yyyy");

            _context.Notifications.Add(new Notification
            {
                UserID    = salary.UserID,
                Message   = $"✅ Your salary for {monthName} has been approved. Amount: ₹{salary.FinalSalary}",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserID     = salary.UserID,
                Action     = $"Salary approved for {monthName}",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to employee ────────────────────────────────────────
            var salaryEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserID == salary.UserID);
            if (salaryEmp != null && !string.IsNullOrWhiteSpace(salaryEmp.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        salaryEmp.Email,
                        $"Salary Approved for {monthName} — CMRL Workforce",
                        $@"<p>Hi {salaryEmp.FullName},</p>
                           <p>Your salary for <b>{monthName}</b> has been <b style='color:#2e7d32'>approved</b>.</p>
                           <p style='font-size:18px;font-weight:bold;color:#2e7d32'>Final Amount: ₹{salary.FinalSalary}</p>"
                    );
                }
                catch { /* Email failure should never block salary approval */ }
            }

            return Ok(new { message = "Salary approved successfully" });
        }
    }

    public class SalaryCalculateRequest
    {
        public int UserID { get; set; }
        public int Month  { get; set; }
        public int Year   { get; set; }
    }
}