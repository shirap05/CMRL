using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;
using CMRL.API.Services;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public EmployeeController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Shift)
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            return Ok(employee);
        }

        [HttpGet("user/{userID}")]
        public async Task<IActionResult> GetByUserID(int userID)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.UserID == userID);

            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            return Ok(employee);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeCreateRequest request)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Employee");
            if (role == null)
                return BadRequest(new { message = "Employee role not found" });

            var user = new User
            {
                Username     = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Email        = request.Email,
                RoleID       = role.RoleID,
                IsActive     = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var employee = new Employee
            {
                UserID       = user.UserID,
                FullName     = request.FullName,
                DepartmentID = request.DepartmentID,
                Designation  = request.Designation,
                Phone        = request.Phone,
                Email        = request.Email,
                BasicSalary  = request.BasicSalary,
                ShiftID      = request.ShiftID,
                ManagerID    = request.ManagerID,
                HRID         = request.HRID,
                IsActive     = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserID     = user.UserID,
                Action     = $"Employee {request.FullName} added",
                ActionTime = DateTime.UtcNow
            });

            // ── Notify assigned HR ────────────────────────────────────────────
            HR? assignedHR = null;
            if (request.HRID.HasValue && request.HRID.Value > 0)
            {
                assignedHR = await _context.HRs.FirstOrDefaultAsync(h => h.HRID == request.HRID.Value);
                if (assignedHR != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserID    = assignedHR.UserID,
                        Message   = $"👤 New employee '{request.FullName}' has been added and assigned to you.",
                        IsRead    = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // ── Notify assigned Manager ───────────────────────────────────────
            Manager? assignedManager = null;
            if (request.ManagerID.HasValue && request.ManagerID.Value > 0)
            {
                assignedManager = await _context.Managers
                    .FirstOrDefaultAsync(m => m.ManagerID == request.ManagerID.Value);
                if (assignedManager != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserID    = assignedManager.UserID,
                        Message   = $"👤 New employee '{request.FullName}' has been added to your team.",
                        IsRead    = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // ── Send emails to HR and Manager ─────────────────────────────────
            if (assignedHR != null && !string.IsNullOrWhiteSpace(assignedHR.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        assignedHR.Email,
                        "New Employee Added — CMRL Workforce",
                        $@"<p>Hi {assignedHR.FullName},</p>
                           <p>A new employee has been added and assigned to you:</p>
                           <p><b>Name:</b> {request.FullName}<br/>
                              <b>Designation:</b> {request.Designation}<br/>
                              <b>Email:</b> {request.Email}<br/>
                              <b>Phone:</b> {request.Phone}</p>"
                    );
                }
                catch { /* Email failure should never block employee creation */ }
            }

            if (assignedManager != null && !string.IsNullOrWhiteSpace(assignedManager.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        assignedManager.Email,
                        "New Employee Added to Your Team — CMRL Workforce",
                        $@"<p>Hi {assignedManager.FullName},</p>
                           <p>A new employee has been added to your team:</p>
                           <p><b>Name:</b> {request.FullName}<br/>
                              <b>Designation:</b> {request.Designation}<br/>
                              <b>Email:</b> {request.Email}<br/>
                              <b>Phone:</b> {request.Phone}</p>"
                    );
                }
                catch { /* Email failure should never block employee creation */ }
            }

            return Ok(new { message = "Employee created successfully", employeeID = employee.EmployeeID });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeUpdateRequest request)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            employee.FullName     = request.FullName;
            employee.DepartmentID = request.DepartmentID;
            employee.Designation  = request.Designation;
            employee.Phone        = request.Phone;
            employee.Email        = request.Email;
            employee.BasicSalary  = request.BasicSalary;
            employee.ShiftID      = request.ShiftID;
            employee.ManagerID    = request.ManagerID;
            employee.HRID         = request.HRID;
            employee.IsActive     = request.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Employee updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Employee deleted successfully" });
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _context.Departments.ToListAsync();
            return Ok(departments);
        }

        [HttpGet("shifts")]
        public async Task<IActionResult> GetShifts()
        {
            var shifts = await _context.Shifts.ToListAsync();
            return Ok(shifts);
        }
    }

    public class EmployeeCreateRequest
    {
        public string  Username     { get; set; } = string.Empty;
        public string  Password     { get; set; } = string.Empty;
        public string  FullName     { get; set; } = string.Empty;
        public int     DepartmentID { get; set; }
        public string  Designation  { get; set; } = string.Empty;
        public string  Phone        { get; set; } = string.Empty;
        public string  Email        { get; set; } = string.Empty;
        public decimal BasicSalary  { get; set; }
        public int     ShiftID      { get; set; }
        public int?    ManagerID    { get; set; }
        public int?    HRID         { get; set; }
    }

    public class EmployeeUpdateRequest
    {
        public string  FullName     { get; set; } = string.Empty;
        public int     DepartmentID { get; set; }
        public string  Designation  { get; set; } = string.Empty;
        public string  Phone        { get; set; } = string.Empty;
        public string  Email        { get; set; } = string.Empty;
        public decimal BasicSalary  { get; set; }
        public int     ShiftID      { get; set; }
        public int?    ManagerID    { get; set; }
        public int?    HRID         { get; set; }
        public bool    IsActive     { get; set; }
    }
}