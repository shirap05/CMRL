using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CMRL.API.Data;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "User not found"
                });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new
                {
                    message = "User is inactive"
                });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Password mismatch"
                });
            }

            // ── Role gate: the role selected on the login screen MUST match
            //    the account's actual role. This stops an Employee's
            //    credentials from working on the HR/Manager/Admin login page
            //    (and vice versa), even though username+password are correct. ──
            if (!string.IsNullOrWhiteSpace(request.ExpectedRole))
            {
                var actualRoleName = user.Role!.RoleName;
                if (!string.Equals(actualRoleName, request.ExpectedRole, StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized(new
                    {
                        message = $"This account is registered as '{actualRoleName}'. Please log in from the {actualRoleName} portal."
                    });
                }
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                userID = user.UserID,
                username = user.Username,
                email = user.Email,
                role = user.Role!.RoleName
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest(new { message = "Username already exists" });

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == request.Role);
            if (role == null)
                return BadRequest(new { message = "Invalid role" });

            var user = new CMRL.API.Models.User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Email = request.Email,
                RoleID = role.RoleID,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User created successfully",
                userID = user.UserID
            });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserID);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "Old password is incorrect" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        private string GenerateJwtToken(CMRL.API.Models.User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role!.RoleName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(jwtSettings["ExpiryMinutes"])
                ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Optional: the role the user picked on the login screen (admin/hr/manager/employee).
        // If provided, the backend enforces that it matches the account's real role.
        public string? ExpectedRole { get; set; }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public int UserID { get; set; }
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}