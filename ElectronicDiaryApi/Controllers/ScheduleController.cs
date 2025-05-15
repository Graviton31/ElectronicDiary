// API Controllers/ScheduleController.cs
using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto.Shedule;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

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

        [HttpGet("{date}")]
        public ActionResult<UnifiedScheduleResponseDto> GetAllGroupsSchedule(DateTime date)
        {
            var startOfWeek = date.Date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(6);

            var startOfWeekDateOnly = DateOnly.FromDateTime(startOfWeek);
            var endOfWeekDateOnly = DateOnly.FromDateTime(endOfWeek);

            var groups = _context.Groups
                .Include(g => g.IdLocationNavigation)
                .Include(g => g.IdSubjectNavigation)
                .Include(g => g.IdEmployees)
                .Include(g => g.StandardSchedules)
                .Include(g => g.ScheduleChanges)
                    .ThenInclude(c => c.IdScheduleNavigation)
                .ToList();

            var unifiedSchedule = InitializeWeekSchedule(startOfWeekDateOnly, endOfWeekDateOnly);

            foreach (var group in groups)
            {
                ProcessGroupSchedule(group, unifiedSchedule, startOfWeekDateOnly, endOfWeekDateOnly);
            }

            return Ok(new UnifiedScheduleResponseDto
            {
                WeekStartDate = startOfWeekDateOnly,
                WeekEndDate = endOfWeekDateOnly,
                Days = unifiedSchedule.Values.OrderBy(d => d.Date).ToList()
            });
        }

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

        private void ProcessGroupSchedule(
            Group group,
            Dictionary<DateOnly, DayScheduleDto> schedule,
            DateOnly weekStart,
            DateOnly weekEnd)
        {
            var standardSchedules = group.StandardSchedules;
            var changes = group.ScheduleChanges
                .Where(c => (c.NewDate >= weekStart && c.NewDate <= weekEnd) ||
                            (c.OldDate >= weekStart && c.OldDate <= weekEnd))
                .ToList();

            // Add standard lessons
            foreach (var s in standardSchedules)
            {
                var originalDate = weekStart.AddDays(s.WeekDay - 1);
                if (originalDate > weekEnd) continue;

                if (!changes.Any(c => c.IdSchedule == s.IdStandardSchedule && c.ChangeType == "перенос"))
                {
                    AddStandardLesson(schedule, group, s, originalDate);
                }
            }

            // Process changes
            foreach (var change in changes)
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

        private void AddStandardLesson(
            Dictionary<DateOnly, DayScheduleDto> schedule,
            Group group,
            StandardSchedule standardSchedule,
            DateOnly date)
        {
            var lesson = new UnifiedLessonInfoDto
            {
                GroupName = group.Name,
                SubjectName = group.IdSubjectNavigation.Name,
                Teachers = group.IdEmployees.Select(e => $"{e.Surname} {e.Name} {e.Patronymic}").ToList(),
                StartTime = standardSchedule.StartTime,
                EndTime = standardSchedule.EndTime,
                Classroom = standardSchedule.Classroom,
                IsChanged = false
            };

            schedule[date].Lessons.Add(lesson);
            schedule[date].Lessons = schedule[date].Lessons
                .OrderBy(l => l.StartTime)
                .ToList();
        }

        private void ProcessReschedule(
            Dictionary<DateOnly, DayScheduleDto> schedule,
            Group group,
            ScheduleChange change)
        {
            // Remove from old date
            if (change.OldDate.HasValue && schedule.ContainsKey(change.OldDate.Value))
            {
                var oldLessons = schedule[change.OldDate.Value].Lessons
                    .Where(l => l.GroupName == group.Name &&
                               l.StartTime == change.IdScheduleNavigation?.StartTime)
                    .ToList();

                foreach (var lesson in oldLessons)
                {
                    schedule[change.OldDate.Value].Lessons.Remove(lesson);
                }
            }

            // Add to new date
            if (change.NewDate.HasValue && schedule.ContainsKey(change.NewDate.Value))
            {
                var newLesson = new UnifiedLessonInfoDto
                {
                    GroupName = group.Name,
                    SubjectName = group.IdSubjectNavigation.Name,
                    Teachers = group.IdEmployees.Select(e => $"{e.Surname} {e.Name} {e.Patronymic}").ToList(),
                    StartTime = change.NewStartTime ?? change.IdScheduleNavigation?.StartTime ?? TimeOnly.MinValue,
                    EndTime = change.NewEndTime ?? change.IdScheduleNavigation?.EndTime ?? TimeOnly.MinValue,
                    Classroom = change.NewClassroom ?? change.IdScheduleNavigation?.Classroom,
                    IsChanged = true,
                    ChangeType = "перенос",
                    OriginalDetails = new OriginalLessonDetailsDto
                    {
                        Date = change.OldDate ?? DateOnly.MinValue,
                        StartTime = change.IdScheduleNavigation?.StartTime ?? TimeOnly.MinValue,
                        EndTime = change.IdScheduleNavigation?.EndTime ?? TimeOnly.MinValue
                    }
                };

                schedule[change.NewDate.Value].Lessons.Add(newLesson);
                schedule[change.NewDate.Value].Lessons = schedule[change.NewDate.Value].Lessons
                    .OrderBy(l => l.StartTime)
                    .ToList();
            }
        }

        private void ProcessCancellation(
            Dictionary<DateOnly, DayScheduleDto> schedule,
            Group group,
            ScheduleChange change)
        {
            if (!change.OldDate.HasValue || !schedule.ContainsKey(change.OldDate.Value)) return;

            var lessons = schedule[change.OldDate.Value].Lessons
                .Where(l => l.GroupName == group.Name &&
                           l.StartTime == change.IdScheduleNavigation?.StartTime)
                .ToList();

            foreach (var lesson in lessons)
            {
                lesson.IsCancelled = true;
                lesson.ChangeType = "отмена";
            }
        }

        private void ProcessAdditionalLesson(
            Dictionary<DateOnly, DayScheduleDto> schedule,
            Group group,
            ScheduleChange change)
        {
            if (!change.NewDate.HasValue || !schedule.ContainsKey(change.NewDate.Value)) return;

            var lesson = new UnifiedLessonInfoDto
            {
                GroupName = group.Name,
                SubjectName = group.IdSubjectNavigation.Name,
                Teachers = group.IdEmployees.Select(e => $"{e.Surname} {e.Name} {e.Patronymic}").ToList(),
                StartTime = change.NewStartTime ?? TimeOnly.MinValue,
                EndTime = change.NewEndTime ?? TimeOnly.MinValue,
                Classroom = change.NewClassroom ?? string.Empty,
                IsChanged = true,
                ChangeType = "дополнительное",
                IsAdditional = true
            };

            schedule[change.NewDate.Value].Lessons.Add(lesson);
            schedule[change.NewDate.Value].Lessons = schedule[change.NewDate.Value].Lessons
                .OrderBy(l => l.StartTime)
                .ToList();
        }
    }
}