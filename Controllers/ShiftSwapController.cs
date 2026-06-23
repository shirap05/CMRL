using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;
using CMRL.API.Services;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShiftSwapController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public ShiftSwapController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requests = await _context.ShiftSwapRequests
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
            return Ok(requests);
        }

        [HttpGet("user/{userID}")]
        public async Task<IActionResult> GetByUser(int userID)
        {
            var requests = await _context.ShiftSwapRequests
                .Where(r => r.UserID == userID)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
            return Ok(requests);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var requests = await _context.ShiftSwapRequests
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
            return Ok(requests);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShiftSwapCreateRequest request)
        {
            var swap = new ShiftSwapRequest
            {
                UserID = request.UserID,
                CurrentShiftID = request.CurrentShiftID,
                RequestedShiftID = request.RequestedShiftID,
                Reason = request.Reason,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };
            _context.ShiftSwapRequests.Add(swap);

            // Notify the employee's HR
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserID == request.UserID);
            string empName = employee?.FullName ?? $"User {request.UserID}";

            if (employee?.HRID != null)
            {
                var hr = await _context.HRs.FirstOrDefaultAsync(h => h.HRID == employee.HRID);
                if (hr != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserID = hr.UserID,
                        Message = $"🔄 {empName} has requested a shift change. Please review.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            _context.Notifications.Add(new Notification
            {
                UserID = request.UserID,
                Message = $"🔄 Your shift change request has been submitted successfully.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to HR ──────────────────────────────────────────────
            if (employee?.HRID != null)
            {
                var hr = await _context.HRs.FirstOrDefaultAsync(h => h.HRID == employee.HRID);
                if (hr != null && !string.IsNullOrWhiteSpace(hr.Email))
                {
                    try
                    {
                        var currentShift = await _context.Shifts.FirstOrDefaultAsync(s => s.ShiftID == request.CurrentShiftID);
                        var requestedShift = await _context.Shifts.FirstOrDefaultAsync(s => s.ShiftID == request.RequestedShiftID);

                        await _emailService.SendEmailAsync(
                            hr.Email,
                            "New Shift Swap Request — CMRL Workforce",
                            $@"<p>Hi {hr.FullName},</p>
                               <p><b>{empName}</b> has requested a shift change:</p>
                               <p><b>Current Shift:</b> {currentShift?.ShiftName ?? "Unknown"}<br/>
                                  <b>Requested Shift:</b> {requestedShift?.ShiftName ?? "Unknown"}<br/>
                                  <b>Reason:</b> {request.Reason}</p>
                               <p>Please review and approve/reject this request in the CMRL Workforce Portal.</p>"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ EMAIL SEND FAILED (ShiftSwap Create, UserID={request.UserID}, HREmail={hr.Email}): {ex.Message}");
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            return Ok(new { message = "Shift swap request submitted successfully", requestID = swap.RequestID });
        }

        [HttpPut("approve/{id}")]
        public async Task<IActionResult> Approve(int id, [FromBody] ShiftSwapActionRequest request)
        {
            var swap = await _context.ShiftSwapRequests.FindAsync(id);
            if (swap == null)
                return NotFound(new { message = "Request not found" });

            swap.Status = "Approved";
            swap.ApprovedBy = request.ApprovedBy;

            // Apply the shift change
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserID == swap.UserID);
            if (employee != null)
            {
                employee.ShiftID = swap.RequestedShiftID;
            }
            else
            {
                var hr = await _context.HRs.FirstOrDefaultAsync(h => h.UserID == swap.UserID);
                if (hr != null)
                {
                    hr.ShiftID = swap.RequestedShiftID;
                }
                else
                {
                    var manager = await _context.Managers.FirstOrDefaultAsync(m => m.UserID == swap.UserID);
                    if (manager != null)
                    {
                        manager.ShiftID = swap.RequestedShiftID;
                    }
                }
            }

            _context.Notifications.Add(new Notification
            {
                UserID = swap.UserID,
                Message = $"✅ Your shift change request has been approved.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to the requester (Employee/HR/Manager) ──────────────
            try
            {
                string? recipientEmail = null;
                string? recipientName  = null;

                var approvedEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserID == swap.UserID);
                if (approvedEmp != null)
                {
                    recipientEmail = approvedEmp.Email;
                    recipientName  = approvedEmp.FullName;
                }
                else
                {
                    var approvedHr = await _context.HRs.FirstOrDefaultAsync(h => h.UserID == swap.UserID);
                    if (approvedHr != null)
                    {
                        recipientEmail = approvedHr.Email;
                        recipientName  = approvedHr.FullName;
                    }
                    else
                    {
                        var approvedManager = await _context.Managers.FirstOrDefaultAsync(m => m.UserID == swap.UserID);
                        if (approvedManager != null)
                        {
                            recipientEmail = approvedManager.Email;
                            recipientName  = approvedManager.FullName;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(recipientEmail))
                {
                    var requestedShift = await _context.Shifts.FirstOrDefaultAsync(s => s.ShiftID == swap.RequestedShiftID);
                    await _emailService.SendEmailAsync(
                        recipientEmail,
                        "Shift Swap Request Approved — CMRL Workforce",
                        $@"<p>Hi {recipientName},</p>
                           <p>Your shift change request has been <b style='color:#2e7d32'>approved</b>.</p>
                           <p><b>New Shift:</b> {requestedShift?.ShiftName ?? "Unknown"}</p>"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ EMAIL SEND FAILED (ShiftSwap Approve, UserID={swap.UserID}): {ex.Message}");
                Console.WriteLine(ex.ToString());
            }

            return Ok(new { message = "Shift swap approved successfully" });
        }

        [HttpPut("reject/{id}")]
        public async Task<IActionResult> Reject(int id, [FromBody] ShiftSwapActionRequest request)
        {
            var swap = await _context.ShiftSwapRequests.FindAsync(id);
            if (swap == null)
                return NotFound(new { message = "Request not found" });

            swap.Status = "Rejected";
            swap.ApprovedBy = request.ApprovedBy;

            _context.Notifications.Add(new Notification
            {
                UserID = swap.UserID,
                Message = $"❌ Your shift change request has been rejected.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // ── Send email to the requester (Employee/HR/Manager) ──────────────
            try
            {
                string? recipientEmail = null;
                string? recipientName  = null;

                var rejectedEmp = await _context.Employees.FirstOrDefaultAsync(e => e.UserID == swap.UserID);
                if (rejectedEmp != null)
                {
                    recipientEmail = rejectedEmp.Email;
                    recipientName  = rejectedEmp.FullName;
                }
                else
                {
                    var rejectedHr = await _context.HRs.FirstOrDefaultAsync(h => h.UserID == swap.UserID);
                    if (rejectedHr != null)
                    {
                        recipientEmail = rejectedHr.Email;
                        recipientName  = rejectedHr.FullName;
                    }
                    else
                    {
                        var rejectedManager = await _context.Managers.FirstOrDefaultAsync(m => m.UserID == swap.UserID);
                        if (rejectedManager != null)
                        {
                            recipientEmail = rejectedManager.Email;
                            recipientName  = rejectedManager.FullName;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(recipientEmail))
                {
                    await _emailService.SendEmailAsync(
                        recipientEmail,
                        "Shift Swap Request Rejected — CMRL Workforce",
                        $@"<p>Hi {recipientName},</p>
                           <p>Your shift change request has been <b style='color:#c62828'>rejected</b>.</p>
                           <p>Please contact HR for more details.</p>"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ EMAIL SEND FAILED (ShiftSwap Reject, UserID={swap.UserID}): {ex.Message}");
                Console.WriteLine(ex.ToString());
            }

            return Ok(new { message = "Shift swap rejected successfully" });
        }
    }

    public class ShiftSwapCreateRequest
    {
        public int UserID { get; set; }
        public int CurrentShiftID { get; set; }
        public int RequestedShiftID { get; set; }
        public string? Reason { get; set; }
    }

    public class ShiftSwapActionRequest
    {
        public int ApprovedBy { get; set; }
    }
}