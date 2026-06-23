using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMRL.API.Data;
using CMRL.API.Models;

namespace CMRL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HolidayController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HolidayController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var holidays = await _context.Holidays
                .OrderBy(h => h.HolidayDate)
                .ToListAsync();
            return Ok(holidays);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HolidayCreateRequest request)
        {
            try
            {
                if (await _context.Holidays.AnyAsync(h => h.HolidayDate == DateOnly.Parse(request.HolidayDate)))
                    return BadRequest(new { message = "A holiday already exists on this date." });

                var holiday = new Holiday
                {
                    HolidayDate = DateOnly.Parse(request.HolidayDate),
                    HolidayName = request.HolidayName,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Holidays.Add(holiday);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Holiday added successfully", holidayID = holiday.HolidayID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Server error: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null)
                return NotFound(new { message = "Holiday not found" });

            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Holiday deleted successfully" });
        }
    }

    public class HolidayCreateRequest
    {
        public string HolidayDate { get; set; } = string.Empty;
        public string HolidayName { get; set; } = string.Empty;
    }
}