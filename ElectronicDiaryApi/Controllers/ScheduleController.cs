using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto.Responses;
using ElectronicDiaryApi.ModelsDto.Shedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public ScheduleController(ElectronicDiaryContext context)
        {
            _context = context;
        }

        [HttpGet("{date}/{personal}")]
        [Authorize]
        public ActionResult<UnifiedScheduleResponseDto> GetSchedule(DateTime date, bool personal = true)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    personal = false;
                }

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRole = User.FindFirstValue(ClaimTypes.Role);

                if (!int.TryParse(userIdStr, out int userId) || string.IsNullOrEmpty(userRole))
                {
                    personal = false;
                }

                var startOfWeek = date.Date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
                var endOfWeek = startOfWeek.AddDays(6);

                var groups = _context.Groups
                    .Include(g => g.IdLocationNavigation)
                    .Include(g => g.IdSubjectNavigation)
                    .Include(g => g.IdEmployees)
                        .ThenInclude(e => e.IdEmployeeNavigation)
                    .Include(g => g.IdStudents)
                        .ThenInclude(s => s.IdStudentNavigation)
                    .Include(g => g.StandardSchedules)
                    .Include(g => g.ScheduleChanges)
                        .ThenInclude(c => c.IdScheduleNavigation)
                    .AsQueryable();

                bool hasPersonalSchedule = true;

                if (personal)
                {
                    switch (userRole.ToLower())
                    {
                        case "учитель":
                            var teacherGroups = _context.Groups
                                .Any(g => g.IdEmployees.Any(e => e.IdEmployee == userId));

                            if (teacherGroups)
                            {
                                groups = groups.Where(g => g.IdEmployees.Any(e => e.IdEmployee == userId));
                                hasPersonalSchedule = true;
                                personal = true;
                            }
                            else
                            {
                                hasPersonalSchedule = false;
                                personal = false;
                            }
                            break;

                        case "родитель":
                            // Для родителя всегда показываем личное расписание при personal=true
                            groups = GetGroupsForParent(userId);
                            hasPersonalSchedule = groups.Any();

                            // Если нет данных для родителя, переключаем на общее расписание
                            if (!hasPersonalSchedule)
                            {
                                personal = false;
                                groups = _context.Groups
                                    .Include(g => g.IdLocationNavigation)
                                    .Include(g => g.IdSubjectNavigation)
                                    .Include(g => g.IdEmployees)
                                        .ThenInclude(e => e.IdEmployeeNavigation)
                                    .Include(g => g.IdStudents)
                                        .ThenInclude(s => s.IdStudentNavigation)
                                    .Include(g => g.StandardSchedules)
                                    .Include(g => g.ScheduleChanges)
                                        .ThenInclude(c => c.IdScheduleNavigation);
                            }
                            break;

                        case "студент":
                            groups = groups.Where(g => g.IdStudents.Any(s => s.IdStudent == userId));
                            hasPersonalSchedule = groups.Any();
                            break;

                        default:
                            personal = false;
                            break;
                    }
                }

                var unifiedSchedule = InitializeWeekSchedule(
                    DateOnly.FromDateTime(startOfWeek),
                    DateOnly.FromDateTime(endOfWeek));

                foreach (var group in groups.ToList())
                {
                    ProcessGroupSchedule(group, unifiedSchedule,
                        DateOnly.FromDateTime(startOfWeek),
                        DateOnly.FromDateTime(endOfWeek));

                    // Добавляем информацию о детях для родителя
                    if (userRole.ToLower() == "родитель" && personal)
                    {
                        AddChildrenInfoToLessons(unifiedSchedule, userId, group.IdGroup);
                    }
                }

                return Ok(new UnifiedScheduleResponseDto
                {
                    WeekStartDate = DateOnly.FromDateTime(startOfWeek),
                    WeekEndDate = DateOnly.FromDateTime(endOfWeek),
                    Days = unifiedSchedule.Values.OrderBy(d => d.Date).ToList(),
                    HasPersonalSchedule = hasPersonalSchedule,
                    IsPersonalMode = personal
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("group/{groupId}/week/{date}")]
        public ActionResult<UnifiedScheduleResponseDto> GetSingleGroupSchedule(int groupId, DateTime date)
        {
            var group = _context.Groups
                .Include(g => g.IdLocationNavigation)
                .Include(g => g.IdSubjectNavigation)
                .Include(g => g.IdEmployees)
                    .ThenInclude(e => e.IdEmployeeNavigation)
                .Include(g => g.StandardSchedules)
                .Include(g => g.ScheduleChanges)
                    .ThenInclude(c => c.IdScheduleNavigation)
                .FirstOrDefault(g => g.IdGroup == groupId);

            if (group == null)
            {
                return NotFound("Group not found");
            }

            var startOfWeek = date.Date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(6);

            var startOfWeekDateOnly = DateOnly.FromDateTime(startOfWeek);
            var endOfWeekDateOnly = DateOnly.FromDateTime(endOfWeek);

            var unifiedSchedule = InitializeWeekSchedule(startOfWeekDateOnly, endOfWeekDateOnly);

            ProcessGroupSchedule(group, unifiedSchedule, startOfWeekDateOnly, endOfWeekDateOnly);

            return Ok(new UnifiedScheduleResponseDto
            {
                WeekStartDate = startOfWeekDateOnly,
                WeekEndDate = endOfWeekDateOnly,
                Days = unifiedSchedule.Values.OrderBy(d => d.Date).ToList()
            });
        }

        // Метод для получения групп, где учатся дети родителя
        private IQueryable<Group> GetGroupsForParent(int parentId)
        {
            // Получаем ID всех детей родителя
            var childrenIds = _context.StudentsHasParents
                .Where(shp => shp.IdParent == parentId)
                .Select(shp => shp.IdStudent)
                .ToList();

            // Возвращаем группы, где есть хотя бы один ребенок родителя
            return _context.Groups
                .Include(g => g.IdLocationNavigation)
                .Include(g => g.IdSubjectNavigation)
                .Include(g => g.IdEmployees)
                    .ThenInclude(e => e.IdEmployeeNavigation)
                .Include(g => g.IdStudents)
                    .ThenInclude(s => s.IdStudentNavigation)
                .Include(g => g.StandardSchedules)
                .Include(g => g.ScheduleChanges)
                    .ThenInclude(c => c.IdScheduleNavigation)
                .Where(g => g.IdStudents.Any(s => childrenIds.Contains(s.IdStudent)));
        }

        // Метод для добавления информации о детях к урокам
        private void AddChildrenInfoToLessons(
            Dictionary<DateOnly, DayScheduleDto> schedule,
            int parentId,
            int groupId)
        {
            // Получаем имена всех детей родителя, которые ходят в эту группу
            var childrenInGroup = _context.StudentsHasParents
                .Include(shp => shp.IdStudentNavigation)
                    .ThenInclude(s => s.IdStudentNavigation)
                .Where(shp => shp.IdParent == parentId &&
                             shp.IdStudentNavigation.IdGroups.Any(g => g.IdGroup == groupId))
                .Select(shp => $"{shp.IdStudentNavigation.IdStudentNavigation.Surname} " +
                              $"{shp.IdStudentNavigation.IdStudentNavigation.Name}")
                .Distinct()
                .ToList();

            // Добавляем информацию о детях к каждому уроку в этой группе
            foreach (var day in schedule.Values)
            {
                foreach (var lesson in day.Lessons.Where(l => l.GroupId == groupId))
                {
                    lesson.ChildrenInfo = childrenInGroup;
                }
            }
        }

        // Инициализация структуры расписания на неделю
        private Dictionary<DateOnly, DayScheduleDto> InitializeWeekSchedule(DateOnly start, DateOnly end)
        {
            var schedule = new Dictionary<DateOnly, DayScheduleDto>();
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                schedule[day] = new DayScheduleDto
                {
                    Date = day,
                    DayOfWeek = day.DayOfWeek.ToString(),
                    Lessons = new List<UnifiedLessonInfoDto>()
                };
            }
            return schedule;
        }

        // Обработка расписания для конкретной группы
        private void ProcessGroupSchedule(
            Group group,
            Dictionary<DateOnly, DayScheduleDto> schedule,
            DateOnly weekStart,
            DateOnly weekEnd)
        {
            // Обработка стандартного расписания
            foreach (var s in group.StandardSchedules)
            {
                var originalDate = weekStart.AddDays(s.WeekDay - 1);
                if (originalDate > weekEnd) continue;

                if (!group.ScheduleChanges.Any(c => c.IdSchedule == s.IdStandardSchedule && c.ChangeType == "перенос"))
                {
                    AddStandardLesson(schedule, group, s, originalDate);
                }
            }

            // Обработка изменений в расписании
            foreach (var change in group.ScheduleChanges
                .Where(c => (c.NewDate >= weekStart && c.NewDate <= weekEnd) ||
                            (c.OldDate >= weekStart && c.OldDate <= weekEnd))
                .ToList())
            {
                switch (change.ChangeType)
                {
                    case "перенос":
                        ProcessReschedule(schedule, group, change);
                        break;
                    case "отмена":
                        ProcessCancellation(schedule, group, change);
                        break;
                    case "дополнительное":
                        ProcessAdditionalLesson(schedule, group, change);
                        break;
                }
            }
        }

        // Добавление стандартного урока
        private void AddStandardLesson(
            Dictionary<DateOnly, DayScheduleDto> schedule,
            Group group,
            StandardSchedule standardSchedule,
            DateOnly date)
        {
            var lesson = new UnifiedLessonInfoDto
            {
                GroupId = group.IdGroup,
                GroupName = group.Name,
                SubjectName = group.IdSubjectNavigation.Name,
                Teachers = group.IdEmployees.Select(e =>
                    FormatFullName(
                        e.IdEmployeeNavigation.Surname,
                        e.IdEmployeeNavigation.Name,
                        e.IdEmployeeNavigation.Patronymic))
                    .ToList(),
                StartTime = standardSchedule.StartTime,
                EndTime = standardSchedule.EndTime,
                Classroom = standardSchedule.Classroom,
                Location = group.IdLocationNavigation?.Name,
                StandardScheduleId = standardSchedule.IdStandardSchedule,
                IsChanged = false
            };

            schedule[date].Lessons.Add(lesson);
            schedule[date].Lessons = schedule[date].Lessons
                .OrderBy(l => l.StartTime)
                .ToList();
        }

        // Обработка переноса урока
        private void ProcessReschedule(
            Dictionary<DateOnly, DayScheduleDto> schedule,
            Group group,
            ScheduleChange change)
        {
            // Удаление с старой даты
            if (change.OldDate.HasValue && schedule.ContainsKey(change.OldDate.Value))
            {
                var oldLessons = schedule[change.OldDate.Value].Lessons
                    .Where(l => l.GroupId == group.IdGroup &&
                               l.StartTime == change.IdScheduleNavigation?.StartTime)
                    .ToList();

                foreach (var lesson in oldLessons)
                {
                    schedule[change.OldDate.Value].Lessons.Remove(lesson);
                }
            }

            // Добавление на новую дату
            if (change.NewDate.HasValue && schedule.ContainsKey(change.NewDate.Value))
            {
                var newLesson = new UnifiedLessonInfoDto
                {
                    GroupId = group.IdGroup,
                    GroupName = group.Name,
                    SubjectName = group.IdSubjectNavigation.Name,
                    Teachers = group.IdEmployees.Select(e =>
                        FormatFullName(
                            e.IdEmployeeNavigation.Surname,
                            e.IdEmployeeNavigation.Name,
                            e.IdEmployeeNavigation.Patronymic))
                        .ToList(),
                    StartTime = change.NewStartTime ?? change.IdScheduleNavigation?.StartTime ?? TimeOnly.MinValue,
                    EndTime = change.NewEndTime ?? change.IdScheduleNavigation?.EndTime ?? TimeOnly.MinValue,
                    Classroom = change.NewClassroom ?? change.IdScheduleNavigation?.Classroom,
                    Location = group.IdLocationNavigation?.Name,
                    IsChanged = true,
                    ChangeType = "перенос",
                    ScheduleChangeId = change.IdScheduleChange,
                    OriginalDetails = new OriginalLessonDetailsDto
                    {
                        Date = change.OldDate ?? DateOnly.MinValue,
                        StartTime = change.IdScheduleNavigation?.StartTime ?? TimeOnly.MinValue,
                        EndTime = change.IdScheduleNavigation?.EndTime ?? TimeOnly.MinValue,
                        Classroom = change.IdScheduleNavigation?.Classroom ?? string.Empty
                    }
                };

                schedule[change.NewDate.Value].Lessons.Add(newLesson);
                schedule[change.NewDate.Value].Lessons = schedule[change.NewDate.Value].Lessons
                    .OrderBy(l => l.StartTime)
                    .ToList();
            }
        }

        // Обработка отмены урока
        private void ProcessCancellation(
            Dictionary<DateOnly, DayScheduleDto> schedule,
            Group group,
            ScheduleChange change)
        {
            if (!change.OldDate.HasValue || !schedule.ContainsKey(change.OldDate.Value)) return;

            var originalStartTime = change.IdScheduleNavigation?.StartTime;
            if (originalStartTime == null) return;

            var lessons = schedule[change.OldDate.Value].Lessons
                .Where(l => l.GroupName == group.Name &&
                           l.StartTime == originalStartTime)
                .ToList();

            foreach (var lesson in lessons)
            {
                lesson.IsCancelled = true;
                lesson.ChangeType = "отмена";
                lesson.ScheduleChangeId = change.IdScheduleChange;
            }
        }

        // Обработка дополнительного урока
        private void ProcessAdditionalLesson(
            Dictionary<DateOnly, DayScheduleDto> schedule,
            Group group,
            ScheduleChange change)
        {
            if (!change.NewDate.HasValue || !schedule.ContainsKey(change.NewDate.Value)) return;

            var lesson = new UnifiedLessonInfoDto
            {
                GroupId = group.IdGroup,
                GroupName = group.Name,
                SubjectName = group.IdSubjectNavigation.Name,
                Teachers = group.IdEmployees.Select(e =>
                    FormatFullName(
                        e.IdEmployeeNavigation.Surname,
                        e.IdEmployeeNavigation.Name,
                        e.IdEmployeeNavigation.Patronymic))
                    .ToList(),
                StartTime = change.NewStartTime ?? TimeOnly.MinValue,
                EndTime = change.NewEndTime ?? TimeOnly.MinValue,
                Classroom = change.NewClassroom ?? string.Empty,
                IsChanged = true,
                ChangeType = "дополнительное",
                ScheduleChangeId = change.IdScheduleChange,
                IsAdditional = true
            };

            schedule[change.NewDate.Value].Lessons.Add(lesson);
            schedule[change.NewDate.Value].Lessons = schedule[change.NewDate.Value].Lessons
                .OrderBy(l => l.StartTime)
                .ToList();
        }

        private string FormatFullName(string surname, string name, string patronymic)
        {
            if (string.IsNullOrEmpty(patronymic))
            {
                return $"{surname} {name[0]}.";
            }
            return $"{surname} {name[0]}. {patronymic[0]}.";
        }

    }
}