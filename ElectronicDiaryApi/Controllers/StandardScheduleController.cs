// Controllers/StandardScheduleController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;

namespace ElectronicDiaryApi.Controllers
{
    [ApiController]
    [Route("api/standard-schedules")]
    public class StandardScheduleController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public StandardScheduleController(ElectronicDiaryContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StandardScheduleResponse>> GetById(int id)
        {
            var schedule = await _context.StandardSchedules
                .Include(s => s.IdGroupNavigation)
                .FirstOrDefaultAsync(s => s.IdStandardSchedule == id);

            return schedule == null
                ? NotFound()
                : Ok(MapToResponse(schedule));
        }

        [HttpPost]
        public async Task<ActionResult<StandardScheduleResponse>> Create(
            [FromBody] StandardScheduleRequest request)
        {
            if (!await _context.Groups.AnyAsync(g => g.IdGroup == request.GroupId))
                return BadRequest("Group not found");

            var newSchedule = new StandardSchedule
            {
                IdGroup = request.GroupId,
                WeekDay = request.WeekDay,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Classroom = request.Classroom
            };

            _context.StandardSchedules.Add(newSchedule);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById),
                new { id = newSchedule.IdStandardSchedule },
                MapToResponse(newSchedule));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id, [FromBody] StandardScheduleRequest request)
        {
            var schedule = await _context.StandardSchedules.FindAsync(id);
            if (schedule == null) return NotFound();

            schedule.WeekDay = request.WeekDay;
            schedule.StartTime = request.StartTime;
            schedule.EndTime = request.EndTime;
            schedule.Classroom = request.Classroom;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.StandardSchedules
                .Include(s => s.ScheduleChanges)
                .FirstOrDefaultAsync(s => s.IdStandardSchedule == id);

            if (schedule == null) return NotFound();
            if (schedule.ScheduleChanges.Any())
                return BadRequest("Cannot delete schedule with existing changes");

            _context.StandardSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetByGroup(int groupId)
        {
            try
            {
                // Получаем стандартные расписания для группы
                var schedules = await _context.StandardSchedules
                    .Where(s => s.IdGroup == groupId)
                    .Select(s => new StandardScheduleResponse
                    {
                        Id = s.IdStandardSchedule,
                        GroupId = s.IdGroup,
                        WeekDay = s.WeekDay,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Classroom = s.Classroom
                    })
                    .ToListAsync();

                return Ok(schedules);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                return StatusCode(500, "Internal Server Error");
            }
        }

        private static StandardScheduleResponse MapToResponse(StandardSchedule schedule)
        {
            return new StandardScheduleResponse
            {
                Id = schedule.IdStandardSchedule,
                GroupId = schedule.IdGroup,
                WeekDay = schedule.WeekDay,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Classroom = schedule.Classroom
            };
        }

        public class StandardScheduleRequest
        {
            public int GroupId { get; set; }
            public sbyte WeekDay { get; set; }
            public TimeOnly StartTime { get; set; }
            public TimeOnly EndTime { get; set; }
            public string? Classroom { get; set; }
        }

        public class StandardScheduleResponse
        {
            public int Id { get; set; }
            public int GroupId { get; set; }
            public sbyte WeekDay { get; set; }
            public TimeOnly StartTime { get; set; }
            public TimeOnly EndTime { get; set; }
            public string? Classroom { get; set; }
        }
    }
}