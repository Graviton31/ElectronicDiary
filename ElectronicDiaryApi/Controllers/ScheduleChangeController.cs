// Controllers/ScheduleChangeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;

namespace ElectronicDiaryApi.Controllers
{
    [ApiController]
    [Route("api/ScheduleChanges")]
    public class ScheduleChangeController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public ScheduleChangeController(ElectronicDiaryContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduleChangeResponse>> GetById(int id)
        {
            var change = await _context.ScheduleChanges
                .Include(c => c.IdScheduleNavigation)
                .FirstOrDefaultAsync(c => c.IdScheduleChange == id);

            return change == null
                ? NotFound()
                : Ok(MapToResponse(change));
        }

        [HttpPost]
        public async Task<ActionResult<ScheduleChangeResponse>> Create(
            [FromBody] ScheduleChangeRequest request)
        {
            if (!await _context.Groups.AnyAsync(g => g.IdGroup == request.GroupId))
                return BadRequest("Group not found");

            var change = new ScheduleChange
            {
                IdGroup = request.GroupId,
                ChangeType = request.ChangeType,
                OldDate = request.OldDate,
                NewDate = request.NewDate,
                NewStartTime = request.NewStartTime,
                NewEndTime = request.NewEndTime,
                NewClassroom = request.NewClassroom,
                IdSchedule = request.StandardScheduleId
            };

            _context.ScheduleChanges.Add(change);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById),
                new { id = change.IdScheduleChange },
                MapToResponse(change));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id, [FromBody] ScheduleChangeRequest request)
        {
            var change = await _context.ScheduleChanges.FindAsync(id);
            if (change == null) return NotFound();

            change.ChangeType = request.ChangeType;
            change.OldDate = request.OldDate;
            change.NewDate = request.NewDate;
            change.NewStartTime = request.NewStartTime;
            change.NewEndTime = request.NewEndTime;
            change.NewClassroom = request.NewClassroom;
            change.IdSchedule = request.StandardScheduleId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var change = await _context.ScheduleChanges.FindAsync(id);
            if (change == null) return NotFound();

            _context.ScheduleChanges.Remove(change);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static ScheduleChangeResponse MapToResponse(ScheduleChange change)
        {
            return new ScheduleChangeResponse
            {
                Id = change.IdScheduleChange,
                GroupId = change.IdGroup,
                ChangeType = change.ChangeType,
                OldDate = change.OldDate,
                NewDate = change.NewDate,
                NewStartTime = change.NewStartTime,
                NewEndTime = change.NewEndTime,
                NewClassroom = change.NewClassroom,
                StandardScheduleId = change.IdSchedule
            };
        }

        public class ScheduleChangeRequest
        {
            public int GroupId { get; set; }
            public string ChangeType { get; set; }
            public DateOnly? OldDate { get; set; }
            public DateOnly? NewDate { get; set; }
            public TimeOnly? NewStartTime { get; set; }
            public TimeOnly? NewEndTime { get; set; }
            public string? NewClassroom { get; set; }
            public int? StandardScheduleId { get; set; }
        }

        public class ScheduleChangeResponse
        {
            public int Id { get; set; }
            public int GroupId { get; set; }
            public string ChangeType { get; set; }
            public DateOnly? OldDate { get; set; }
            public DateOnly? NewDate { get; set; }
            public TimeOnly? NewStartTime { get; set; }
            public TimeOnly? NewEndTime { get; set; }
            public string? NewClassroom { get; set; }
            public int? StandardScheduleId { get; set; }
        }
    }
}