// Controllers/StandardScheduleController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto.Shedule;

namespace ElectronicDiaryApi.Controllers
{
    [ApiController]
    [Route("api/StandardSchedules")]
    public class StandardScheduleController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;
        private readonly ILogger<ElectronicDiaryContext> _logger;

        public StandardScheduleController(ElectronicDiaryContext context, ILogger<ElectronicDiaryContext> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("GetStandardSchedules")]
        public async Task<IActionResult> GetStandardSchedules(
           [FromQuery] int groupId,
           [FromQuery] DateTime date)
        {
            try
            {
                var weekday = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;

                var schedules = await _context.StandardSchedules
                    .Where(s => s.IdGroup == groupId && s.WeekDay == weekday)
                    .Select(s => new
                    {
                        Id = s.IdStandardSchedule,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Classroom = s.Classroom
                    })
                    .ToListAsync();

                return Ok(schedules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StandardScheduleDto>> GetById(int id)
        {
            var schedule = await _context.StandardSchedules
                .FirstOrDefaultAsync(s => s.IdStandardSchedule == id);

            if (schedule == null) return NotFound();

            return new StandardScheduleDto
            {
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Classroom = schedule.Classroom
            };
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

        // В StandardScheduleController.cs
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"Attempting to delete standard schedule with ID: {id}");

            var schedule = await _context.StandardSchedules
                .Include(s => s.ScheduleChanges)
                .FirstOrDefaultAsync(s => s.IdStandardSchedule == id);

            if (schedule == null)
            {
                _logger.LogWarning($"Standard schedule with ID {id} not found");
                return NotFound(new { error = "Расписание не найдено" });
            }

            // Удаляем связанные изменения расписания
            if (schedule.ScheduleChanges.Any())
            {
                _context.ScheduleChanges.RemoveRange(schedule.ScheduleChanges);
            }

            _context.StandardSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully deleted standard schedule with ID: {id} with all related changes");
            return NoContent();
        }

        [HttpGet("{id}/hasChanges")]
        public async Task<IActionResult> HasChanges(int id)
        {
            var hasChanges = await _context.ScheduleChanges
                .AnyAsync(c => c.IdSchedule == id);

            return Ok(new { hasChanges });
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