using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using System.Globalization;

namespace ElectronicDiaryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupScheduleController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public GroupScheduleController(ElectronicDiaryContext context)
        {
            _context = context;
        }

        // GET: Получение стандартного расписания по ID
        [HttpGet("standard/{scheduleId}")]
        public async Task<ActionResult<StandardLessonDto>> GetStandardSchedule(int scheduleId)
        {
            var schedule = await _context.StandardSchedules.FindAsync(scheduleId);
            if (schedule == null) return NotFound();
            return MapStandardToDto(schedule);
        }

        // GET: Получение изменения расписания по ID
        [HttpGet("changes/{changeId}")]
        public async Task<ActionResult<ScheduleChangeDto>> GetScheduleChange(int changeId)
        {
            var change = await _context.ScheduleChanges.FindAsync(changeId);
            if (change == null) return NotFound();
            return MapChangeToDto(change);
        }

        // GET: Получение полного расписания группы на неделю
        [HttpGet("{groupId}")]
        public async Task<ActionResult<GroupScheduleResponseDto>> GetGroupSchedule(int groupId, [FromQuery] DateTime date)
        {
            var group = await _context.Groups
                .Include(g => g.StandardSchedules)
                .Include(g => g.ScheduleChanges)
                    .ThenInclude(c => c.IdScheduleNavigation)
                .FirstOrDefaultAsync(g => g.IdGroup == groupId);

            if (group == null) return NotFound("Группа не найдена");

            var startOfWeek = date.Date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(6);

            var schedule = new GroupScheduleResponseDto
            {
                GroupId = groupId,
                GroupName = group.Name,
                WeekStartDate = DateOnly.FromDateTime(startOfWeek),
                WeekEndDate = DateOnly.FromDateTime(endOfWeek),
                Days = new List<GroupDayScheduleDto>()
            };

            // Инициализация дней недели
            for (var d = startOfWeek; d <= endOfWeek; d = d.AddDays(1))
            {
                schedule.Days.Add(new GroupDayScheduleDto
                {
                    Date = DateOnly.FromDateTime(d),
                    DayOfWeek = d.ToString("dddd", new CultureInfo("ru-RU")),
                    StandardLessons = new List<StandardLessonDto>(),
                    Changes = new List<ScheduleChangeDto>()
                });
            }

            // Добавление стандартных занятий
            foreach (var standard in group.StandardSchedules)
            {
                var day = schedule.Days.FirstOrDefault(d =>
                    (int)d.Date.DayOfWeek == (standard.WeekDay == 7 ? 0 : standard.WeekDay));

                if (day != null)
                {
                    day.StandardLessons.Add(new StandardLessonDto
                    {
                        Id = standard.IdStandardSchedule,
                        StartTime = standard.StartTime,
                        EndTime = standard.EndTime,
                        Classroom = standard.Classroom
                    });
                }
            }

            // Добавление изменений
            foreach (var change in group.ScheduleChanges
                .Where(c => c.NewDate >= schedule.WeekStartDate && c.NewDate <= schedule.WeekEndDate ||
                            c.OldDate >= schedule.WeekStartDate && c.OldDate <= schedule.WeekEndDate))
            {
                var changeDto = new ScheduleChangeDto
                {
                    Id = change.IdScheduleChange,
                    ChangeType = change.ChangeType,
                    OldDate = change.OldDate,
                    NewDate = change.NewDate,
                    NewStartTime = change.NewStartTime,
                    NewEndTime = change.NewEndTime,
                    NewClassroom = change.NewClassroom,
                    RelatedStandardScheduleId = change.IdSchedule
                };

                var day = schedule.Days.FirstOrDefault(d => d.Date == change.NewDate);
                if (day != null) day.Changes.Add(changeDto);
            }

            return Ok(schedule);
        }

        // POST: Добавление стандартного расписания
        [HttpPost("{groupId}/standard")]
        public async Task<ActionResult<StandardLessonDto>> CreateStandardSchedule(
            int groupId, [FromBody] StandardScheduleCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return NotFound("Группа не найдена");

            var newSchedule = new StandardSchedule
            {
                IdGroup = groupId,
                WeekDay = dto.WeekDay,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Classroom = dto.Classroom
            };

            _context.StandardSchedules.Add(newSchedule);
            await _context.SaveChangesAsync();

            // Исправленная ссылка на метод
            return CreatedAtAction(nameof(GetStandardSchedule),
                new { scheduleId = newSchedule.IdStandardSchedule },
                MapStandardToDto(newSchedule));
        }

        // PUT: Обновление стандартного расписания
        [HttpPut("standard/{scheduleId}")]
        public async Task<IActionResult> UpdateStandardSchedule(
            int scheduleId, [FromBody] StandardScheduleUpdateDto dto)
        {
            var schedule = await _context.StandardSchedules.FindAsync(scheduleId);
            if (schedule == null) return NotFound();

            schedule.WeekDay = dto.WeekDay;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;
            schedule.Classroom = dto.Classroom;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: Удаление стандартного расписания
        [HttpDelete("standard/{scheduleId}")]
        public async Task<IActionResult> DeleteStandardSchedule(int scheduleId)
        {
            var schedule = await _context.StandardSchedules
                .Include(s => s.ScheduleChanges)
                .FirstOrDefaultAsync(s => s.IdStandardSchedule == scheduleId);

            if (schedule == null) return NotFound();

            if (schedule.ScheduleChanges.Any())
                return BadRequest("Нельзя удалить расписание с связанными изменениями");

            _context.StandardSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: Создание изменения расписания
        [HttpPost("{groupId}/changes")]
        public async Task<ActionResult<ScheduleChangeDto>> CreateScheduleChange(
    int groupId, [FromBody] ScheduleChangeCreateDto dto)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return NotFound("Группа не найдена");

            var change = new ScheduleChange
            {
                IdGroup = groupId,
                ChangeType = dto.ChangeType,
                OldDate = dto.OldDate,
                NewDate = dto.NewDate,
                NewStartTime = dto.NewStartTime,
                NewEndTime = dto.NewEndTime,
                NewClassroom = dto.NewClassroom,
                IdSchedule = dto.StandardScheduleId
            };

            _context.ScheduleChanges.Add(change);
            await _context.SaveChangesAsync();

            // Исправленная ссылка на метод
            return CreatedAtAction(nameof(GetScheduleChange),
                new { changeId = change.IdScheduleChange },
                MapChangeToDto(change));
        }

        // PUT: Обновление изменения расписания
        [HttpPut("changes/{changeId}")]
        public async Task<IActionResult> UpdateScheduleChange(
            int changeId, [FromBody] ScheduleChangeUpdateDto dto)
        {
            var change = await _context.ScheduleChanges.FindAsync(changeId);
            if (change == null) return NotFound();

            change.ChangeType = dto.ChangeType;
            change.OldDate = dto.OldDate;
            change.NewDate = dto.NewDate;
            change.NewStartTime = dto.NewStartTime;
            change.NewEndTime = dto.NewEndTime;
            change.NewClassroom = dto.NewClassroom;
            change.IdSchedule = dto.StandardScheduleId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: Удаление изменения расписания
        [HttpDelete("changes/{changeId}")]
        public async Task<IActionResult> DeleteScheduleChange(int changeId)
        {
            var change = await _context.ScheduleChanges.FindAsync(changeId);
            if (change == null) return NotFound();

            _context.ScheduleChanges.Remove(change);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private StandardLessonDto MapStandardToDto(StandardSchedule schedule)
        {
            return new StandardLessonDto
            {
                Id = schedule.IdStandardSchedule,
                WeekDay = schedule.WeekDay,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Classroom = schedule.Classroom
            };
        }

        private ScheduleChangeDto MapChangeToDto(ScheduleChange change)
        {
            return new ScheduleChangeDto
            {
                Id = change.IdScheduleChange,
                ChangeType = change.ChangeType,
                OldDate = change.OldDate,
                NewDate = change.NewDate,
                NewStartTime = change.NewStartTime,
                NewEndTime = change.NewEndTime,
                NewClassroom = change.NewClassroom,
                RelatedStandardScheduleId = change.IdSchedule
            };
        }

        public class GroupScheduleResponseDto
        {
            public int GroupId { get; set; }
            public string GroupName { get; set; }
            public DateOnly WeekStartDate { get; set; }
            public DateOnly WeekEndDate { get; set; }
            public List<GroupDayScheduleDto> Days { get; set; }
        }

        public class GroupDayScheduleDto
        {
            public DateOnly Date { get; set; }
            public string DayOfWeek { get; set; }
            public List<StandardLessonDto> StandardLessons { get; set; }
            public List<ScheduleChangeDto> Changes { get; set; }
        }

        public class StandardLessonDto
        {
            public int Id { get; set; }
            public sbyte WeekDay { get; set; }
            public TimeOnly StartTime { get; set; }
            public TimeOnly EndTime { get; set; }
            public string? Classroom { get; set; }
        }

        public class ScheduleChangeDto
        {
            public int Id { get; set; }
            public string ChangeType { get; set; }
            public DateOnly? OldDate { get; set; }
            public DateOnly? NewDate { get; set; }
            public TimeOnly? NewStartTime { get; set; }
            public TimeOnly? NewEndTime { get; set; }
            public string NewClassroom { get; set; }
            public int? RelatedStandardScheduleId { get; set; }
        }

        // Request DTOs
        public class StandardScheduleCreateDto
        {
            public sbyte WeekDay { get; set; } // 1-7 (Пн-Вс)
            public TimeOnly StartTime { get; set; }
            public TimeOnly EndTime { get; set; }
            public string? Classroom { get; set; }
        }

        public class StandardScheduleUpdateDto
        {
            public sbyte WeekDay { get; set; }
            public TimeOnly StartTime { get; set; }
            public TimeOnly EndTime { get; set; }
            public string? Classroom { get; set; }
        }

        public class ScheduleChangeCreateDto
        {
            public string ChangeType { get; set; }
            public DateOnly? OldDate { get; set; }
            public DateOnly? NewDate { get; set; }
            public TimeOnly? NewStartTime { get; set; }
            public TimeOnly? NewEndTime { get; set; }
            public string? NewClassroom { get; set; }
            public int? StandardScheduleId { get; set; }
        }

        public class ScheduleChangeUpdateDto
        {
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