using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AttendanceController(AppDbContext context)
        {
            _context = context;
        }
        [HttpPost("backfill-absences")]
        public async Task<IActionResult> BackfillAbsences()
        {
            var istZone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
            var todayIst = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone));
            var yesterday = todayIst.AddDays(-1); // never backfill today, only completed days

            var holidaySet = (await _context.Holidays.Select(h => h.HolidayDate).ToListAsync()).ToHashSet();

            var employeeUsers = await _context.Employees.Where(e => e.IsActive).Select(e => e.UserID).ToListAsync();
            var hrUsers        = await _context.HRs.Where(h => h.IsActive).Select(h => h.UserID).ToListAsync();
            var managerUsers   = await _context.Managers.Where(m => m.IsActive).Select(m => m.UserID).ToListAsync();
            var allUserIds     = employeeUsers.Concat(hrUsers).Concat(managerUsers).Distinct().ToList();

            int totalInserted = 0;

            foreach (var userId in allUserIds)
            {
                // Determine join date from earliest "added" audit log, fallback to earliest attendance row
                var joinLog = await _context.AuditLogs
                    .Where(l => l.UserID == userId && l.Action.Contains("added"))
                    .OrderBy(l => l.ActionTime)
                    .FirstOrDefaultAsync();

                var earliestAttendance = await _context.Attendances
                    .Where(a => a.UserID == userId)
                    .OrderBy(a => a.AttendanceDate)
                    .Select(a => a.AttendanceDate)
                    .FirstOrDefaultAsync();

                DateOnly joinDate = joinLog != null
                    ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(joinLog.ActionTime.ToUniversalTime(), istZone))
                    : (earliestAttendance != default ? earliestAttendance : yesterday);

                if (joinDate > yesterday) continue;

                var existingDates = (await _context.Attendances
                    .Where(a => a.UserID == userId && a.AttendanceDate >= joinDate && a.AttendanceDate <= yesterday)
                    .Select(a => a.AttendanceDate)
                    .ToListAsync()).ToHashSet();

                var approvedLeaves = await _context.Leaves
                    .Where(l => l.UserID == userId && l.Status == "Approved")
                    .ToListAsync();

                var leaveDates = new HashSet<DateOnly>();
                foreach (var lv in approvedLeaves)
                {
                    for (var d = lv.FromDate; d <= lv.ToDate; d = d.AddDays(1))
                        leaveDates.Add(d);
                }

                for (var d = joinDate; d <= yesterday; d = d.AddDays(1))
                {
                    if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday) continue;
                    if (holidaySet.Contains(d)) continue;
                    if (existingDates.Contains(d)) continue;
                    if (leaveDates.Contains(d)) continue;

                    _context.Attendances.Add(new Attendance
                    {
                        UserID = userId,
                        AttendanceDate = d,
                        Status = "Absent",
                        IsLate = false
                    });
                    totalInserted++;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Backfill complete. {totalInserted} absent records created.", totalInserted });
        }

        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
        {
            // ── Geo-fence check ───────────────────────────────────────────────
            // Allowed zones: CMRL HQ | VIT Campus
            var allowedZones = new[]
            {
                new { Name = "CMRL HQ",    Lat = 12.839702471843779, Lon = 80.1529757105113,  RadiusKm = 1.0 },
                new { Name = "VIT Campus", Lat = 13.031960512692176, Lon = 80.24388061303823, RadiusKm = 1.0 }
            };

            bool isWithinAnyZone = allowedZones.Any(zone =>
                GetDistance(request.Latitude, request.Longitude, zone.Lat, zone.Lon) <= zone.RadiusKm
            );

            if (!isWithinAnyZone)
                return BadRequest(new { message = "You are outside authorized location. Attendance cannot be marked." });

            // ── Already checked in today? ─────────────────────────────────────
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var existing = await _context.Attendances
                .FirstOrDefaultAsync(a => a.UserID == request.UserID && a.AttendanceDate == today);

            if (existing != null && existing.CheckInTime != null)
                return BadRequest(new { message = "Already checked in today" });

            // ── Late check ───────────────────────────────────────────────────
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserID == request.UserID);
            bool isLate = false;

            if (employee != null)
            {
                var shift = await _context.Shifts.FindAsync(employee.ShiftID);
                if (shift != null)
                {
                    var currentTime = TimeOnly.FromDateTime(DateTime.UtcNow);
                    isLate = currentTime > shift.StartTime.AddMinutes(15);
                }
            }

            // ── Save attendance ───────────────────────────────────────────────
            if (existing == null)
            {
                _context.Attendances.Add(new Attendance
                {
                    UserID = request.UserID,
                    AttendanceDate = today,
                    CheckInTime = DateTime.UtcNow,
                    Status = "Present",
                    IsLate = isLate
                });
            }
            else
            {
                existing.CheckInTime = DateTime.UtcNow;
                existing.Status = "Present";
                existing.IsLate = isLate;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserID = request.UserID,
                Action = $"User {request.UserID} checked in",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = isLate ? "Checked in - Late" : "Checked in - On Time", isLate });
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.UserID == request.UserID && a.AttendanceDate == today);

            if (attendance == null || attendance.CheckInTime == null)
                return BadRequest(new { message = "You have not checked in today" });

            if (attendance.CheckOutTime != null)
                return BadRequest(new { message = "Already checked out today" });

            attendance.CheckOutTime = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                UserID = request.UserID,
                Action = $"User {request.UserID} checked out",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Checked out successfully" });
        }

        [HttpGet("user/{userID}")]
        public async Task<IActionResult> GetByUser(int userID)
        {
            var attendances = await _context.Attendances
                .Where(a => a.UserID == userID)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
            return Ok(attendances);
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetToday()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var attendances = await _context.Attendances
                .Where(a => a.AttendanceDate == today)
                .ToListAsync();
            return Ok(attendances);
        }

        [HttpGet("summary/{userID}")]
        public async Task<IActionResult> GetSummary(int userID)
        {
            var attendances = await _context.Attendances
                .Where(a => a.UserID == userID)
                .ToListAsync();

            // ── Determine join date from earliest "added" audit log ───────────
            var joinLog = await _context.AuditLogs
                .Where(l => l.UserID == userID && l.Action.Contains("added"))
                .OrderBy(l => l.ActionTime)
                .FirstOrDefaultAsync();

            DateOnly joinDate = joinLog != null
                ? DateOnly.FromDateTime(joinLog.ActionTime)
                : (attendances.Any()
                    ? attendances.Min(a => a.AttendanceDate)
                    : DateOnly.FromDateTime(DateTime.UtcNow));

            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

            // ── Approved leave dates (so they don't count as Absent) ──────────
            var approvedLeaves = await _context.Leaves
                .Where(l => l.UserID == userID && l.Status == "Approved")
                .ToListAsync();

            var leaveDates = new HashSet<DateOnly>();
            foreach (var lv in approvedLeaves)
            {
                for (var d = lv.FromDate; d <= lv.ToDate; d = d.AddDays(1))
                    leaveDates.Add(d);
            }

            var presentDates = attendances
                .Where(a => a.Status == "Present")
                .Select(a => a.AttendanceDate)
                .ToHashSet();

            // ── Holiday dates (so they don't count as Absent either) ──────────
            var holidayDates = await _context.Holidays
                .Select(h => h.HolidayDate)
                .ToListAsync();
            var holidaySet = holidayDates.ToHashSet();

            // ── Count working days (Mon–Fri, excluding holidays) from join date through today ─────
            int totalWorkingDays = 0;
            int absentDays = 0;

            for (var d = joinDate; d <= today; d = d.AddDays(1))
            {
                bool isWeekend = d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
                if (isWeekend) continue;

                bool isHoliday = holidaySet.Contains(d);
                if (isHoliday) continue;

                totalWorkingDays++;

                bool isPresent = presentDates.Contains(d);
                bool isOnLeave = leaveDates.Contains(d);

                if (!isPresent && !isOnLeave)
                    absentDays++;
            }

            int presentDays  = attendances.Count(a => a.Status == "Present");
            int lateEntries  = attendances.Count(a => a.IsLate);
            double attendancePercentage = totalWorkingDays > 0
                ? Math.Round((double)presentDays / totalWorkingDays * 100, 2)
                : 0;

            var summary = new
            {
                TotalDays            = totalWorkingDays,
                PresentDays          = presentDays,
                AbsentDays           = absentDays,
                LateEntries          = lateEntries,
                AttendancePercentage = attendancePercentage,
                JoinDate             = joinDate.ToString("yyyy-MM-dd")
            };

            return Ok(summary);
        }

        [HttpGet("allusers")]
        public async Task<IActionResult> GetAllUsersAttendance()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Shift)
                .ToListAsync();

            var hrs = await _context.HRs
                .Include(h => h.Shift)
                .ToListAsync();

            var managers = await _context.Managers
                .Include(m => m.Department)
                .Include(m => m.Shift)
                .ToListAsync();

            var todayAttendance = await _context.Attendances
                .Where(a => a.AttendanceDate == today)
                .ToListAsync();

            var allManagers = await _context.Managers.ToListAsync();
            var allHRs = await _context.HRs.ToListAsync();

            var employeeData = employees.Select(e => {
                var att = todayAttendance.FirstOrDefault(a => a.UserID == e.UserID);
                var mgr = allManagers.FirstOrDefault(m => m.ManagerID == e.ManagerID);
                var hr = allHRs.FirstOrDefault(h => h.HRID == e.HRID);
                return new {
                    e.EmployeeID,
                    e.FullName,
                    Department = e.Department?.DepartmentName ?? "",
                    e.DepartmentID,
                    e.Designation,
                    e.BasicSalary,
                    e.Phone,
                    e.Email,
                    ManagerName = mgr?.FullName ?? "",
                    e.ManagerID,
                    HRName = hr?.FullName ?? "",
                    e.HRID,
                    Shift = e.Shift?.ShiftName ?? "",
                    e.ShiftID,
                    e.IsActive,
                    CheckInTime = att?.CheckInTime,
                    CheckOutTime = att?.CheckOutTime,
                    Status = att?.Status ?? "Absent"
                };
            }).ToList();

            var hrData = hrs.Select(h => {
                var att = todayAttendance.FirstOrDefault(a => a.UserID == h.UserID);
                return new {
                    h.HRID,
                    h.FullName,
                    h.BasicSalary,
                    h.Phone,
                    h.Email,
                    Shift = h.Shift?.ShiftName ?? "",
                    h.ShiftID,
                    h.IsActive,
                    CheckInTime = att?.CheckInTime,
                    CheckOutTime = att?.CheckOutTime,
                    Status = att?.Status ?? "Absent"
                };
            }).ToList();

            var managerData = managers.Select(m => {
                var att = todayAttendance.FirstOrDefault(a => a.UserID == m.UserID);
                return new {
                    m.ManagerID,
                    m.FullName,
                    Department = m.Department?.DepartmentName ?? "",
                    m.DepartmentID,
                    m.BasicSalary,
                    m.Phone,
                    m.Email,
                    Shift = m.Shift?.ShiftName ?? "",
                    m.ShiftID,
                    m.IsActive,
                    CheckInTime = att?.CheckInTime,
                    CheckOutTime = att?.CheckOutTime,
                    Status = att?.Status ?? "Absent"
                };
            }).ToList();

            return Ok(new {
                employees = employeeData,
                hrs = hrData,
                managers = managerData,
                totalEmployees = employees.Count,
                activeEmployees = todayAttendance.Count(a => employees.Any(e => e.UserID == a.UserID) && a.Status == "Present"),
                inactiveEmployees = employees.Count - todayAttendance.Count(a => employees.Any(e => e.UserID == a.UserID) && a.Status == "Present"),
                totalHR = hrs.Count,
                activeHR = todayAttendance.Count(a => hrs.Any(h => h.UserID == a.UserID) && a.Status == "Present"),
                inactiveHR = hrs.Count - todayAttendance.Count(a => hrs.Any(h => h.UserID == a.UserID) && a.Status == "Present"),
                totalManagers = managers.Count,
                activeManagers = todayAttendance.Count(a => managers.Any(m => m.UserID == a.UserID) && a.Status == "Present"),
                inactiveManagers = managers.Count - todayAttendance.Count(a => managers.Any(m => m.UserID == a.UserID) && a.Status == "Present")
            });
        }

        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }

    public class CheckInRequest
    {
        public int UserID { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class CheckOutRequest
    {
        public int UserID { get; set; }
    }
}