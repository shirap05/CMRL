using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HRController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HRController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/HR
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var hrs = await _context.HRs
                .Include(h => h.Shift)
                .ToListAsync();
            return Ok(hrs);
        }

        // GET: api/HR/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var hr = await _context.HRs
                .Include(h => h.Shift)
                .FirstOrDefaultAsync(h => h.HRID == id);

            if (hr == null)
                return NotFound(new { message = "HR not found" });

            return Ok(hr);
        }

        // POST: api/HR
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HRCreateRequest request)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "HR");
            if (role == null)
                return BadRequest(new { message = "HR role not found" });

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

            var hr = new HR
            {
                UserID = user.UserID,
                FullName = request.FullName,
                Phone = request.Phone,
                Email = request.Email,
                BasicSalary = request.BasicSalary,
                ShiftID = request.ShiftID,
                IsActive = true
            };

            _context.HRs.Add(hr);
            await _context.SaveChangesAsync();

            var log = new AuditLog
            {
                UserID = user.UserID,
                Action = $"HR {request.FullName} added",
                ActionTime = DateTime.UtcNow
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "HR created successfully", hrID = hr.HRID });
        }

        // PUT: api/HR/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] HRUpdateRequest request)
        {
            var hr = await _context.HRs.FindAsync(id);
            if (hr == null)
                return NotFound(new { message = "HR not found" });

            hr.FullName = request.FullName;
            hr.Phone = request.Phone;
            hr.Email = request.Email;
            hr.BasicSalary = request.BasicSalary;
            hr.ShiftID = request.ShiftID;
            hr.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { message = "HR updated successfully" });
        }

        // DELETE: api/HR/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var hr = await _context.HRs.FindAsync(id);
            if (hr == null)
                return NotFound(new { message = "HR not found" });

            _context.HRs.Remove(hr);
            await _context.SaveChangesAsync();

            return Ok(new { message = "HR deleted successfully" });
        }
    }

    public class HRCreateRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public int ShiftID { get; set; }
    }

    public class HRUpdateRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public int ShiftID { get; set; }
        public bool IsActive { get; set; }
    }
}