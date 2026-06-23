using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;
using Microsoft.AspNetCore.Authorization;

namespace CMRL.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Notification/user/5
        [HttpGet("user/{userID}")]
        public async Task<IActionResult> GetByUser(int userID)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userID)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return Ok(notifications);
        }

        // GET: api/Notification/unread/5
        [HttpGet("unread/{userID}")]
        public async Task<IActionResult> GetUnread(int userID)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userID && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return Ok(notifications);
        }

        // PUT: api/Notification/read/5
        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification marked as read" });
        }

        // PUT: api/Notification/readall/5
        [HttpPut("readall/{userID}")]
        public async Task<IActionResult> MarkAllAsRead(int userID)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserID == userID && !n.IsRead)
                .ToListAsync();

            notifications.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();

            return Ok(new { message = "All notifications marked as read" });
        }

        // POST: api/Notification
        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendNotificationRequest request)
        {
            var notification = new Notification
            {
                UserID = request.UserID,
                Message = request.Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification sent successfully" });
        }
    }

    public class SendNotificationRequest
    {
        public int UserID { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}