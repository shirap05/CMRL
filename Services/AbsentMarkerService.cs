using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;

namespace CMRL.API.Services
{
    public class AbsentMarkerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AbsentMarkerService> _logger;

        public AbsentMarkerService(IServiceProvider serviceProvider, ILogger<AbsentMarkerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MarkAbsentees();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AbsentMarkerService");
                }

                var istZone = TimeZoneInfo.FindSystemTimeZoneById(
                    OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
                var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
                var nextRun = nowIst.Date.AddDays(1).AddHours(1);
                var delay = nextRun - nowIst;
                if (delay.TotalMilliseconds < 0) delay = TimeSpan.FromHours(1);

                await System.Threading.Tasks.Task.Delay(delay, stoppingToken);
            }
        }

        private async System.Threading.Tasks.Task MarkAbsentees()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var istZone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
            var todayIst = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone));
            var yesterday = todayIst.AddDays(-1);

            var dow = yesterday.DayOfWeek;
            if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday)
                return;

            var isHoliday = await context.Holidays.AnyAsync(h => h.HolidayDate == yesterday);
            if (isHoliday)
                return;

            var employeeUserIds = await context.Employees.Where(e => e.IsActive).Select(e => e.UserID).ToListAsync();
            var hrUserIds       = await context.HRs.Where(h => h.IsActive).Select(h => h.UserID).ToListAsync();
            var managerUserIds  = await context.Managers.Where(m => m.IsActive).Select(m => m.UserID).ToListAsync();

            var allUserIds = employeeUserIds.Concat(hrUserIds).Concat(managerUserIds).Distinct().ToList();

            var existingUserIds = await context.Attendances
                .Where(a => a.AttendanceDate == yesterday)
                .Select(a => a.UserID)
                .ToListAsync();

            var missingUserIds = allUserIds.Except(existingUserIds).ToList();

            foreach (var userId in missingUserIds)
            {
                var onLeave = await context.Leaves.AnyAsync(l =>
                    l.UserID == userId &&
                    l.Status == "Approved" &&
                    l.FromDate <= yesterday &&
                    l.ToDate >= yesterday);

                if (onLeave)
                    continue;

                context.Attendances.Add(new Attendance
                {
                    UserID = userId,
                    AttendanceDate = yesterday,
                    Status = "Absent",
                    IsLate = false
                });
            }

            if (missingUserIds.Count > 0)
            {
                await context.SaveChangesAsync();
                _logger.LogInformation($"Marked {missingUserIds.Count} users as Absent for {yesterday}");
            }
        }
    }
}