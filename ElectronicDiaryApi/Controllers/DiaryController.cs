using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto.Diary;
using ElectronicDiaryApi.ModelsDto.Journal;
using ElectronicDiaryApi.ModelsDto.Shedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DiaryController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;
        private readonly ILogger<DiaryController> _logger;

        public DiaryController(ElectronicDiaryContext context, ILogger<DiaryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("week/{date}")]
        public async Task<ActionResult<DiaryWeekResponseDto>> GetDiaryWeek(
            DateTime date,
            [FromQuery] int? studentId = null)
        {
            try
            {
                _logger.LogInformation($"Запрос дневника на дату: {date:yyyy-MM-dd}, studentId: {studentId}");

                // 1. Получаем идентификатор и роль пользователя
                var userId = GetUserIdFromClaims();
                if (userId == null)
                {
                    _logger.LogWarning("Не удалось получить ID пользователя из claims");
                    return Unauthorized();
                }

                var role = User.FindFirstValue(ClaimTypes.Role);
                _logger.LogInformation($"Пользователь: ID={userId}, Role={role}");

                // 2. Обработка для родителя
                List<ChildDto> availableChildren = new List<ChildDto>();
                if (role == "родитель")
                {
                    _logger.LogInformation("Обработка запроса для родителя");
                    availableChildren = await GetUserChildren(userId.Value);
                    _logger.LogInformation($"Найдено детей: {availableChildren.Count}");

                    if (availableChildren.Count == 0)
                    {
                        _logger.LogWarning($"У родителя {userId} нет привязанных детей");
                        return Ok(new DiaryWeekResponseDto
                        {
                            AvailableChildren = availableChildren,
                            Days = new List<DiaryDayDto>(),
                        });
                    }

                    // Логирование списка детей
                    foreach (var child in availableChildren)
                    {
                        _logger.LogInformation($"Доступный ребенок: ID={child.Id}, Name={child.FullName}");
                    }

                    // Если studentId не указан - используем первого ребенка
                    if (!studentId.HasValue)
                    {
                        studentId = availableChildren.First().Id;
                        _logger.LogInformation($"Автоматически выбран ребенок: ID={studentId}");
                    }
                    else if (!availableChildren.Any(c => c.Id == studentId))
                    {
                        _logger.LogWarning($"Родитель {userId} не имеет доступа к ребенку {studentId}");
                        return Unauthorized("Нет доступа к указанному ребенку");
                    }
                }
                else if (role == "студент")
                {
                    // Для студента используем его ID
                    studentId = userId;
                    _logger.LogInformation($"Обработка запроса для студента: ID={studentId}");
                }
                else
                {
                    _logger.LogWarning($"Роль {role} не имеет доступа к дневнику");
                    return Forbid();
                }

                // 3. Получаем группы студента с активными журналами
                _logger.LogInformation($"Получение групп и журналов для studentId={studentId}");
                var groupsWithJournals = await _context.Students
                    .Where(s => s.IdStudent == studentId)
                    .SelectMany(s => s.IdGroups)
                    .Include(g => g.IdSubjectNavigation)
                    .Include(g => g.IdEmployees)
                        .ThenInclude(e => e.IdEmployeeNavigation)
                    .Select(g => new GroupJournalInfo
                    {
                        Group = g,
                        ActiveJournal = g.Journals
                            .Where(j => j.StartDate <= DateOnly.FromDateTime(date) &&
                                       (j.EndDate == null || j.EndDate >= DateOnly.FromDateTime(date)))
                            .OrderByDescending(j => j.EndDate)
                            .FirstOrDefault()
                    })
                    .Where(x => x.ActiveJournal != null)
                    .ToListAsync();

                _logger.LogInformation($"Найдено групп с журналами: {groupsWithJournals.Count}");

                if (groupsWithJournals.Count == 0)
                {
                    _logger.LogInformation($"Нет активных журналов для studentId={studentId}");
                    return Ok(new DiaryWeekResponseDto
                    {
                        WeekStartDate = DateOnly.FromDateTime(date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday)),
                        WeekEndDate = DateOnly.FromDateTime(date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday + 6)),
                        Days = new List<DiaryDayDto>(),
                        AvailableChildren = availableChildren,
                        SelectedChildId = studentId,
                    });
                }

                // 4. Получаем расписание с посещениями
                _logger.LogInformation("Получение расписания занятий");
                var schedule = await GetStudentSchedule(groupsWithJournals, date, studentId.Value, role);

                // 5. Формируем ответ
                var response = new DiaryWeekResponseDto
                {
                    WeekStartDate = DateOnly.FromDateTime(schedule.Keys.Min()),
                    WeekEndDate = DateOnly.FromDateTime(schedule.Keys.Max()),
                    Days = schedule.Values.OrderBy(d => d.Date).ToList(),
                    AvailableChildren = availableChildren,
                    SelectedChildId = studentId
                };

                _logger.LogInformation($"Успешно сформирован ответ. Дней с занятиями: {response.Days.Count}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении дневника. Дата: {date:yyyy-MM-dd}, studentId: {studentId}");
                return StatusCode(500, "Произошла ошибка при загрузке данных дневника");
            }
        }

        private async Task<Dictionary<DateTime, DiaryDayDto>> GetStudentSchedule(
            List<GroupJournalInfo> groupsWithJournals,
            DateTime date,
            int studentId,
            string role)
        {
            try
            {
                _logger.LogInformation($"Формирование расписания для studentId={studentId}");

                var weekStart = date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
                var weekEnd = weekStart.AddDays(6);
                _logger.LogInformation($"Неделя с {weekStart:yyyy-MM-dd} по {weekEnd:yyyy-MM-dd}");

                var schedule = InitializeWeek(weekStart, weekEnd);

                // Кэшируем стандартное расписание для всех групп
                var groupIds = groupsWithJournals.Select(g => g.Group.IdGroup).ToList();
                _logger.LogInformation($"Получение стандартного расписания для групп: {string.Join(", ", groupIds)}");

                var standardSchedules = await _context.StandardSchedules
                    .Where(ss => groupIds.Contains(ss.IdGroup))
                    .ToDictionaryAsync(ss => (IdGroup: ss.IdGroup, WeekDay: ss.WeekDay), ss => ss);

                _logger.LogInformation($"Загружено стандартных расписаний: {standardSchedules.Count}");

                foreach (var groupInfo in groupsWithJournals)
                {
                    var group = (Group)groupInfo.Group;
                    var journal = (Journal)groupInfo.ActiveJournal;

                    if (group.IdSubjectNavigation == null)
                    {
                        _logger.LogWarning($"Группа {group.IdGroup} не имеет привязанного предмета");
                        continue;
                    }

                    _logger.LogInformation($"Получение занятий для группы {group.IdGroup}, журнал {journal.IdJournal}");
                    var lessons = await _context.Lessons
                        .Where(l => l.IdJournal == journal.IdJournal &&
                                   l.LessonDate >= DateOnly.FromDateTime(weekStart) &&
                                   l.LessonDate <= DateOnly.FromDateTime(weekEnd))
                        .Include(l => l.Visits.Where(v => v.IdStudent == studentId))
                        .ToListAsync();

                    _logger.LogInformation($"Найдено занятий: {lessons.Count}");

                    foreach (var lesson in lessons)
                    {
                        if (lesson.LessonDate == null)
                        {
                            _logger.LogWarning($"Занятие {lesson.IdLesson} не имеет даты");
                            continue;
                        }

                        var lessonDate = lesson.LessonDate.Value.ToDateTime(TimeOnly.MinValue);
                        if (!schedule.ContainsKey(lessonDate))
                        {
                            schedule[lessonDate] = new DiaryDayDto
                            {
                                Date = lesson.LessonDate.Value,
                                DayOfWeek = lesson.LessonDate.Value.DayOfWeek.ToString(),
                                Lessons = new List<DiaryLessonDto>()
                            };
                        }

                        var visit = lesson.Visits.FirstOrDefault();
                        var visitStatus = visit?.UnvisitedStatuses;
                        var comment = visit?.Comment;

                        // Получаем время занятия
                        var (startTime, endTime) = GetLessonTimes(lesson, group.IdGroup, standardSchedules);

                        _logger.LogInformation($"Добавление занятия: {lessonDate:yyyy-MM-dd}, {startTime}-{endTime}, {group.IdSubjectNavigation.Name}");

                        schedule[lessonDate].Lessons.Add(new DiaryLessonDto
                        {
                            IdLesson = lesson.IdLesson,
                            StartTime = startTime,
                            EndTime = endTime,
                            SubjectName = group.IdSubjectNavigation.Name,
                            GroupName = group.Name,
                            VisitStatus = visitStatus,
                            Comment = role == "родитель" ? comment : null,
                            IsCancelled = false,
                            Teachers = group.IdEmployees.Select(e =>
                                $"{e.IdEmployeeNavigation.Surname} {e.IdEmployeeNavigation.Name}").ToList()
                        });
                    }
                }

                return schedule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при формировании расписания");
                throw;
            }
        }

        private (TimeOnly StartTime, TimeOnly EndTime) GetLessonTimes(Lesson lesson, int groupId,
            Dictionary<(int IdGroup, sbyte WeekDay), StandardSchedule> standardSchedules)
        {
            try
            {
                // 1. Проверяем изменения в расписании
                var scheduleChange = _context.ScheduleChanges
                    .FirstOrDefault(sc => sc.IdGroup == groupId &&
                                        sc.OldDate == lesson.LessonDate);

                if (scheduleChange != null)
                {
                    if (scheduleChange.NewStartTime.HasValue && scheduleChange.NewEndTime.HasValue)
                    {
                        _logger.LogInformation($"Для занятия {lesson.IdLesson} найдено измененное расписание");
                        return (scheduleChange.NewStartTime.Value, scheduleChange.NewEndTime.Value);
                    }
                }

                // 2. Берём из стандартного расписания
                if (lesson.LessonDate.HasValue)
                {
                    var weekDay = (sbyte)lesson.LessonDate.Value.DayOfWeek;
                    if (standardSchedules.TryGetValue((groupId, weekDay), out var standardSchedule))
                    {
                        _logger.LogInformation($"Для занятия {lesson.IdLesson} использовано стандартное расписание");
                        return (standardSchedule.StartTime, standardSchedule.EndTime);
                    }
                }

                // 3. Дефолтные значения
                _logger.LogWarning($"Для занятия {lesson.IdLesson} использовано расписание по умолчанию");
                return (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)),
                        TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении времени занятия {lesson.IdLesson}");
                return (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)),
                        TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)));
            }
        }

        private Dictionary<DateTime, DiaryDayDto> InitializeWeek(DateTime weekStart, DateTime weekEnd)
        {
            var schedule = new Dictionary<DateTime, DiaryDayDto>();
            for (var day = weekStart; day <= weekEnd; day = day.AddDays(1))
            {
                schedule[day] = new DiaryDayDto
                {
                    Date = DateOnly.FromDateTime(day),
                    DayOfWeek = day.DayOfWeek.ToString(),
                    Lessons = new List<DiaryLessonDto>()
                };
            }
            return schedule;
        }

        private async Task<List<ChildDto>> GetUserChildren(int parentId)
        {
            try
            {
                _logger.LogInformation($"Получение детей для родителя {parentId}");

                var children = await _context.StudentsHasParents
                    .Include(shp => shp.IdStudentNavigation)
                    .ThenInclude(s => s.IdStudentNavigation)
                    .Where(shp => shp.IdParent == parentId)
                    .Select(shp => new ChildDto
                    {
                        Id = shp.IdStudent,
                        FullName = shp.IdStudentNavigation.IdStudentNavigation.Surname + " " +
                                  shp.IdStudentNavigation.IdStudentNavigation.Name
                    })
                    .ToListAsync();

                _logger.LogInformation($"Найдено детей: {children.Count}");
                return children;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении детей родителя {parentId}");
                return new List<ChildDto>();
            }
        }

        private int? GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("ClaimTypes.NameIdentifier не найден");
                return null;
            }

            if (!int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning($"Не удалось преобразовать ID пользователя: {userIdClaim}");
                return null;
            }

            return userId;
        }
    }

    public class GroupJournalInfo
    {
        public Group Group { get; set; }
        public Journal ActiveJournal { get; set; }
    }
}