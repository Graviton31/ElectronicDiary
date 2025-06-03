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
        private readonly ILogger<GroupsController> _logger;

        public ScheduleChangeController(ElectronicDiaryContext context, ILogger<GroupsController> logger)
        {
            _context = context;
            _logger = logger;
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _context.Groups.AnyAsync(g => g.IdGroup == request.GroupId))
                return BadRequest("Group not found");

            try
            {
                var change = new ScheduleChange
                {
                    IdGroup = request.GroupId,
                    ChangeType = request.ChangeType,
                    OldDate = request.GetOldDate(),
                    NewDate = request.GetNewDate(),
                    NewStartTime = request.GetNewStartTime(),
                    NewEndTime = request.GetNewEndTime(),
                    NewClassroom = request.NewClassroom,
                    IdSchedule = request.StandardScheduleId
                };

                _context.ScheduleChanges.Add(change);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById),
                    new { id = change.IdScheduleChange },
                    MapToResponse(change));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule change");
                return StatusCode(500);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id, [FromBody] ScheduleChangeRequest request)
        {
            var change = await _context.ScheduleChanges.FindAsync(id);
            if (change == null) return NotFound();

            change.ChangeType = request.ChangeType;
            change.OldDate = request.GetOldDate();
            change.NewDate = request.GetNewDate();
            change.NewStartTime = request.GetNewStartTime();
            change.NewEndTime = request.GetNewEndTime();
            change.NewClassroom = request.NewClassroom;
            change.IdSchedule = request.StandardScheduleId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"Attempting to delete schedule change with ID: {id}");

            var change = await _context.ScheduleChanges.FindAsync(id);
            if (change == null)
            {
                _logger.LogWarning($"Schedule change with ID {id} not found");
                return NotFound();
            }

            _context.ScheduleChanges.Remove(change);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully deleted schedule change with ID: {id}");
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

        // Убрать атрибуты [FromForm] и изменить типы данных
        public class ScheduleChangeRequest
        {
            public int GroupId { get; set; }
            public string ChangeType { get; set; }
            public string? OldDate { get; set; } // Изменено на string
            public string? NewDate { get; set; } // Изменено на string
            public string? NewStartTime { get; set; } // Изменено на string
            public string? NewEndTime { get; set; } // Изменено на string
            public string? NewClassroom { get; set; }
            public int? StandardScheduleId { get; set; }

            // Добавьте методы для конвертации
            public DateOnly? GetOldDate() => OldDate != null ? DateOnly.Parse(OldDate) : null;
            public DateOnly? GetNewDate() => NewDate != null ? DateOnly.Parse(NewDate) : null;
            public TimeOnly? GetNewStartTime() => NewStartTime != null ? TimeOnly.Parse(NewStartTime) : null;
            public TimeOnly? GetNewEndTime() => NewEndTime != null ? TimeOnly.Parse(NewEndTime) : null;
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