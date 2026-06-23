using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;
using CMRL.API.Services;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public TaskController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _context.Tasks
                .OrderByDescending(t => t.TaskID)
                .ToListAsync();
            return Ok(tasks);
        }

        [HttpGet("assigned/{userID}")]
        public async Task<IActionResult> GetAssigned(int userID)
        {
            var tasks = await _context.Tasks
                .Where(t => t.AssignedTo == userID)
                .OrderByDescending(t => t.TaskID)
                .ToListAsync();
            return Ok(tasks);
        }

        [HttpGet("created/{userID}")]
        public async Task<IActionResult> GetCreated(int userID)
        {
            var tasks = await _context.Tasks
                .Where(t => t.AssignedBy == userID)
                .OrderByDescending(t => t.TaskID)
                .ToListAsync();
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskCreateRequest request)
        {
            var task = new Models.Task
            {
                TaskName    = request.TaskName,
                Description = request.Description,
                AssignedTo  = request.AssignedTo,
                AssignedBy  = request.AssignedBy,
                StartDate   = DateOnly.Parse(request.StartDate),
                DueDate     = DateOnly.Parse(request.DueDate),
                Status      = "Pending"
            };
            _context.Tasks.Add(task);

            // ── Notify assigned employee ──────────────────────────────────────
            _context.Notifications.Add(new Notification
            {
                UserID    = request.AssignedTo,
                Message   = $"📋 New task assigned to you: \"{request.TaskName}\" — due {request.DueDate}",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserID     = request.AssignedBy,
                Action     = $"Task '{request.TaskName}' assigned to user {request.AssignedTo}",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to assigned employee ───────────────────────────────
            var assignedEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserID == request.AssignedTo);
            if (assignedEmployee != null && !string.IsNullOrWhiteSpace(assignedEmployee.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        assignedEmployee.Email,
                        "New Task Assigned — CMRL Workforce",
                        $@"<p>Hi {assignedEmployee.FullName},</p>
                           <p>A new task has been assigned to you:</p>
                           <p><b>Task:</b> {request.TaskName}<br/>
                              <b>Description:</b> {request.Description}<br/>
                              <b>Due Date:</b> {request.DueDate}</p>
                           <p>Please log in to the CMRL Workforce Portal to view full details.</p>"
                    );
                }
                catch { /* Email failure should never block task creation */ }
            }

            return Ok(new { message = "Task created successfully", taskID = task.TaskID });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TaskUpdateRequest request)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound(new { message = "Task not found" });

            task.TaskName    = request.TaskName;
            task.Description = request.Description;
            task.AssignedTo  = request.AssignedTo;
            task.StartDate   = DateOnly.Parse(request.StartDate);
            task.DueDate     = DateOnly.Parse(request.DueDate);
            task.Status      = request.Status;

            // ── Notify employee of task update ────────────────────────────────
            _context.Notifications.Add(new Notification
            {
                UserID    = request.AssignedTo,
                Message   = $"📋 Task \"{request.TaskName}\" has been updated.",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Task updated successfully" });
        }

        [HttpPut("status/{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] TaskStatusRequest request)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound(new { message = "Task not found" });

            string oldStatus = task.Status;
            task.Status = request.Status;

            // ── Notify the manager (AssignedBy) when employee updates status ──
            _context.Notifications.Add(new Notification
            {
                UserID    = task.AssignedBy,
                Message   = $"📋 Task \"{task.TaskName}\" status changed to \"{request.Status}\" by assignee.",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            _context.AuditLogs.Add(new AuditLog
            {
                UserID     = task.AssignedTo,
                Action     = $"Task '{task.TaskName}' status updated from {oldStatus} to {request.Status}",
                ActionTime = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Task status updated" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound(new { message = "Task not found" });

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Task deleted successfully" });
        }

        [HttpGet("summary/{userID}")]
        public async Task<IActionResult> GetSummary(int userID)
        {
            var tasks = await _context.Tasks
                .Where(t => t.AssignedBy == userID)
                .ToListAsync();

            return Ok(new
            {
                Total      = tasks.Count,
                Pending    = tasks.Count(t => t.Status == "Pending"),
                InProgress = tasks.Count(t => t.Status == "In Progress"),
                Completed  = tasks.Count(t => t.Status == "Completed")
            });
        }
    }

    public class TaskCreateRequest
    {
        public string TaskName    { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int    AssignedTo  { get; set; }
        public int    AssignedBy  { get; set; }
        public string StartDate   { get; set; } = string.Empty;
        public string DueDate     { get; set; } = string.Empty;
    }

    public class TaskUpdateRequest
    {
        public string TaskName    { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int    AssignedTo  { get; set; }
        public string StartDate   { get; set; } = string.Empty;
        public string DueDate     { get; set; } = string.Empty;
        public string Status      { get; set; } = string.Empty;
    }

    public class TaskStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}