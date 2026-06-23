using Microsoft.EntityFrameworkCore;
using CMRL.API.Models;

namespace CMRL.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<HR> HRs { get; set; }
        public DbSet<Manager> Managers { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Leave> Leaves { get; set; }
        public DbSet<ShiftSwapRequest> ShiftSwapRequests { get; set; }
        public DbSet<Models.Task> Tasks { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<OTPStore> OTPStores { get; set; }
        public DbSet<Holiday> Holidays { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.UserID).HasColumnName("userid");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.PasswordHash).HasColumnName("passwordhash");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.RoleID).HasColumnName("roleid");
                entity.Property(e => e.IsActive).HasColumnName("isactive");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.RoleID);
                entity.Property(e => e.RoleID).HasColumnName("roleid");
                entity.Property(e => e.RoleName).HasColumnName("rolename");
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("departments");
                entity.HasKey(e => e.DepartmentID);
                entity.Property(e => e.DepartmentID).HasColumnName("departmentid");
                entity.Property(e => e.DepartmentName).HasColumnName("departmentname");
            });

            modelBuilder.Entity<Shift>(entity =>
            {
                entity.ToTable("shifts");
                entity.HasKey(e => e.ShiftID);
                entity.Property(e => e.ShiftID).HasColumnName("shiftid");
                entity.Property(e => e.ShiftName).HasColumnName("shiftname");
                entity.Property(e => e.StartTime).HasColumnName("starttime");
                entity.Property(e => e.EndTime).HasColumnName("endtime");
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("employees");
                entity.HasKey(e => e.EmployeeID);
                entity.Property(e => e.EmployeeID).HasColumnName("employeeid");
                entity.Property(e => e.UserID).HasColumnName("userid");
                entity.Property(e => e.FullName).HasColumnName("fullname");
                entity.Property(e => e.DepartmentID).HasColumnName("departmentid");
                entity.Property(e => e.Designation).HasColumnName("designation");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.BasicSalary).HasColumnName("basicsalary");
                entity.Property(e => e.ShiftID).HasColumnName("shiftid");
                entity.Property(e => e.ManagerID).HasColumnName("managerid");
                entity.Property(e => e.HRID).HasColumnName("hrid");
                entity.Property(e => e.IsActive).HasColumnName("isactive");
            });

            modelBuilder.Entity<HR>(entity =>
            {
                entity.ToTable("hr");
                entity.HasKey(e => e.HRID);
                entity.Property(e => e.HRID).HasColumnName("hrid");
                entity.Property(e => e.UserID).HasColumnName("userid");
                entity.Property(e => e.FullName).HasColumnName("fullname");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.BasicSalary).HasColumnName("basicsalary");
                entity.Property(e => e.ShiftID).HasColumnName("shiftid");
                entity.Property(e => e.IsActive).HasColumnName("isactive");
            });

            modelBuilder.Entity<Manager>(entity =>
            {
                entity.ToTable("managers");
                entity.HasKey(e => e.ManagerID);
                entity.Property(e => e.ManagerID).HasColumnName("managerid");
                entity.Property(e => e.UserID).HasColumnName("userid");
                entity.Property(e => e.FullName).HasColumnName("fullname");
                entity.Property(e => e.DepartmentID).HasColumnName("departmentid");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.BasicSalary).HasColumnName("basicsalary");
                entity.Property(e => e.ShiftID).HasColumnName("shiftid");
                entity.Property(e => e.IsActive).HasColumnName("isactive");
            });

            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.ToTable("attendance");
                entity.HasKey(e => e.AttendanceID);
                entity.Property(e => e.AttendanceID).HasColumnName("attendanceid");
                entity.Property(e => e.UserID).HasColumnName("userid");
                entity.Property(e => e.AttendanceDate).HasColumnName("attendancedate");
                entity.Property(e => e.CheckInTime).HasColumnName("checkintime");
                entity.Property(e => e.CheckOutTime).HasColumnName("checkouttime");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.IsLate).HasColumnName("islate");
            });

            modelBuilder.Entity<Leave>(entity =>
            {
                entity.ToTable("leaves");
                entity.HasKey(e => e.LeaveID);
                entity.Property(e => e.LeaveID).HasColumnName("leaveid");
                entity.Property(e => e.UserID).HasColumnName("userid");
                entity.Property(e => e.FromDate).HasColumnName("fromdate");
                entity.Property(e => e.ToDate).HasColumnName("todate");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.ApprovedBy).HasColumnName("approvedby");
                entity.Property(e => e.AppliedAt).HasColumnName("appliedat");
            });

            modelBuilder.Entity<Models.Task>(entity =>
            {
                entity.ToTable("tasks");
                entity.HasKey(e => e.TaskID);
                entity.Property(e => e.TaskID).HasColumnName("taskid");
                entity.Property(e => e.TaskName).HasColumnName("taskname");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.AssignedTo).HasColumnName("assignedto");
                entity.Property(e => e.AssignedBy).HasColumnName("assignedby");
                entity.Property(e => e.StartDate).HasColumnName("startdate");
                entity.Property(e => e.DueDate).HasColumnName("duedate");
                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<Salary>(entity =>
            {
                entity.ToTable("salary");
                entity.HasKey(e => e.SalaryID);
                entity.Property(e => e.SalaryID).HasColumnName("salaryid");
                entity.Property(e => e.UserID).HasColumnName("userid");
                entity.Property(e => e.Month).HasColumnName("month");
                entity.Property(e => e.Year).HasColumnName("year");
                entity.Property(e => e.BasicSalary).HasColumnName("basicsalary");
                entity.Property(e => e.LeaveCount).HasColumnName("leavecount");
                entity.Property(e => e.DeductionAmount).HasColumnName("deductionamount");
                entity.Property(e => e.FinalSalary).HasColumnName("finalsalary");
                entity.Property(e => e.IsApproved).HasColumnName("isapproved");
                entity.Property(e => e.AbsentDays).HasColumnName("absentdays");
                entity.Property(e => e.LateCount).HasColumnName("latecount");
                entity.Property(e => e.LeaveDeduction).HasColumnName("leavededuction");
                entity.Property(e => e.AbsentDeduction).HasColumnName("absentdeduction");
                entity.Property(e => e.LateDeduction).HasColumnName("latededuction");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("notifications");
                entity.HasKey(e => e.NotificationID);
                entity.Property(e => e.NotificationID).HasColumnName("notificationid");
                entity.Property(e => e.UserID).HasColumnName("userid");
                entity.Property(e => e.Message).HasColumnName("message");
                entity.Property(e => e.IsRead).HasColumnName("isread");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("auditlogs");
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.LogID).HasColumnName("logid");
                entity.Property(e => e.UserID).HasColumnName("userid");
                entity.Property(e => e.Action).HasColumnName("action");
                entity.Property(e => e.ActionTime).HasColumnName("actiontime");
            });
            modelBuilder.Entity<OTPStore>(entity =>
{
    entity.ToTable("otpstore");
    entity.HasKey(e => e.OTPID);
    entity.Property(e => e.OTPID).HasColumnName("otpid");
    entity.Property(e => e.UserID).HasColumnName("userid");
    entity.Property(e => e.OTPCode).HasColumnName("otpcode");
    entity.Property(e => e.CreatedAt).HasColumnName("createdat");
    entity.Property(e => e.ExpiresAt).HasColumnName("expiresat");
    entity.Property(e => e.IsUsed).HasColumnName("isused");
});
            modelBuilder.Entity<Setting>(entity =>
            {
                entity.ToTable("settings");
                entity.HasKey(e => e.SettingID);
                entity.Property(e => e.SettingID).HasColumnName("settingid");
                entity.Property(e => e.UserID).HasColumnName("userid");
                entity.Property(e => e.Theme).HasColumnName("theme");
            });
            modelBuilder.Entity<ShiftSwapRequest>(entity =>
{
    entity.ToTable("shiftswaprequests");
    entity.HasKey(e => e.RequestID);
    entity.Property(e => e.RequestID).HasColumnName("requestid");
    entity.Property(e => e.UserID).HasColumnName("userid");
    entity.Property(e => e.CurrentShiftID).HasColumnName("currentshiftid");
    entity.Property(e => e.RequestedShiftID).HasColumnName("requestedshiftid");
    entity.Property(e => e.Reason).HasColumnName("reason");
    entity.Property(e => e.Status).HasColumnName("status");
    entity.Property(e => e.RequestedAt).HasColumnName("requestedat");
    entity.Property(e => e.ApprovedBy).HasColumnName("approvedby");
});
modelBuilder.Entity<Holiday>(entity =>
{
    entity.ToTable("holidays");
    entity.HasKey(e => e.HolidayID);
    entity.Property(e => e.HolidayID).HasColumnName("holidayid");
    entity.Property(e => e.HolidayDate).HasColumnName("holidaydate");
    entity.Property(e => e.HolidayName).HasColumnName("holidayname");
    entity.Property(e => e.CreatedAt).HasColumnName("createdat");
});
        }
    }
}