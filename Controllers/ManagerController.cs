using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManagerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ManagerController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Manager
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var managers = await _context.Managers
                .Include(m => m.Department)
                .Include(m => m.Shift)
                .ToListAsync();
            return Ok(managers);
        }

        // GET: api/Manager/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var manager = await _context.Managers
                .Include(m => m.Department)
                .Include(m => m.Shift)
                .FirstOrDefaultAsync(m => m.ManagerID == id);

            if (manager == null)
                return NotFound(new { message = "Manager not found" });

            return Ok(manager);
        }

        // POST: api/Manager
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ManagerCreateRequest request)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Manager");
            if (role == null)
                return BadRequest(new { message = "Manager role not found" });

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Email = request.Email,
                RoleID = role.RoleID,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var manager = new Manager
            {
                UserID = user.UserID,
                FullName = request.FullName,
                DepartmentID = request.DepartmentID,
                Phone = request.Phone,
                Email = request.Email,
                BasicSalary = request.BasicSalary,
                ShiftID = request.ShiftID,
                IsActive = true
            };

            _context.Managers.Add(manager);
            await _context.SaveChangesAsync();

            var log = new AuditLog
            {
                UserID = user.UserID,
                Action = $"Manager {request.FullName} added",
                ActionTime = DateTime.UtcNow
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Manager created successfully", managerID = manager.ManagerID });
        }

        // PUT: api/Manager/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ManagerUpdateRequest request)
        {
            var manager = await _context.Managers.FindAsync(id);
            if (manager == null)
                return NotFound(new { message = "Manager not found" });

            manager.FullName = request.FullName;
            manager.DepartmentID = request.DepartmentID;
            manager.Phone = request.Phone;
            manager.Email = request.Email;
            manager.BasicSalary = request.BasicSalary;
            manager.ShiftID = request.ShiftID;
            manager.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Manager updated successfully" });
        }

        // DELETE: api/Manager/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var manager = await _context.Managers.FindAsync(id);
            if (manager == null)
                return NotFound(new { message = "Manager not found" });

            _context.Managers.Remove(manager);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Manager deleted successfully" });
        }
    }

    public class ManagerCreateRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int DepartmentID { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public int ShiftID { get; set; }
    }

    public class ManagerUpdateRequest
    {
        public string FullName { get; set; } = string.Empty;
        public int DepartmentID { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public int ShiftID { get; set; }
        public bool IsActive { get; set; }
    }
}