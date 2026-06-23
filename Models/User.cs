namespace CMRL.API.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Role? Role { get; set; }
    }

    public class Role
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class Department
    {
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class Shift
    {
        public int ShiftID { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }

    public class Employee
    {
        public int EmployeeID { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int DepartmentID { get; set; }
        public string Designation { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public int ShiftID { get; set; }
        public int? ManagerID { get; set; }
        public int? HRID { get; set; }
        public bool IsActive { get; set; } = true;
        public Department? Department { get; set; }
        public Shift? Shift { get; set; }
    }

    public class HR
    {
        public int HRID { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public int ShiftID { get; set; }
        public bool IsActive { get; set; } = true;
        public Shift? Shift { get; set; }
    }

    public class Manager
    {
        public int ManagerID { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int DepartmentID { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public int ShiftID { get; set; }
        public bool IsActive { get; set; } = true;
        public Department? Department { get; set; }
        public Shift? Shift { get; set; }
    }

    public class Attendance
    {
        public int AttendanceID { get; set; }
        public int UserID { get; set; }
        public DateOnly AttendanceDate { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; } = "Absent";
        public bool IsLate { get; set; } = false;
    }

    public class Leave
    {
        public int LeaveID { get; set; }
        public int UserID { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public int? ApprovedBy { get; set; }
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }

    public class Task
    {
        public int TaskID { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AssignedTo { get; set; }
        public int AssignedBy { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class Salary
    {
        public int SalaryID { get; set; }
        public int UserID { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal BasicSalary { get; set; }
        public int LeaveCount { get; set; } = 0;
        public decimal DeductionAmount { get; set; } = 0;
        public decimal FinalSalary { get; set; }
        public bool IsApproved { get; set; } = false;

        public int? AbsentDays { get; set; }
        public int? LateCount { get; set; }
        public decimal? LeaveDeduction { get; set; }
        public decimal? AbsentDeduction { get; set; }
        public decimal? LateDeduction { get; set; }
    }

    public class Notification
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        public int LogID { get; set; }
        public int UserID { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime ActionTime { get; set; } = DateTime.UtcNow;
    }

    public class Setting
    {
        public int SettingID { get; set; }
        public int UserID { get; set; }
        public string Theme { get; set; } = "Light";
    }
    public class OTPStore
    {
        public int OTPID { get; set; }
        public int UserID { get; set; }
        public string OTPCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
    }
    public class ShiftSwapRequest
{
    public int RequestID { get; set; }
    public int UserID { get; set; }
    public int CurrentShiftID { get; set; }
    public int RequestedShiftID { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; }
    public int? ApprovedBy { get; set; }
}
public class Holiday
{
    public int HolidayID { get; set; }
    public DateOnly HolidayDate { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
}