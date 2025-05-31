using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto;
using ElectronicDiaryApi.ModelsDto.Group;
using ElectronicDiaryApi.ModelsDto.Journal;
using ElectronicDiaryApi.ModelsDto.Subject;
using ElectronicDiaryApi.ModelsDto.UsersView;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JournalsController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;
        private readonly ILogger<JournalsController> _logger;

        public JournalsController(
            ElectronicDiaryContext context,
            ILogger<JournalsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Получает список предметов, связанных с указанным учителем, если задан.
        /// </summary>
        /// <param name="teacherId">Идентификатор учителя (необязательный).</param>
        /// <returns>Список предметов.</returns>
        [HttpGet("Subjects")]
        public async Task<ActionResult<IEnumerable<SubjectListItemDto>>> GetSubjects(
            [FromQuery] int? teacherId)
        {
            // Формируем запрос к базе данных для получения предметов
            var query = _context.Subjects
                .Include(s => s.IdEmployees) // Включаем связанных учителей
                //.Include(s => s.Groups) // Включаем связанные группы
                .Where(s => s.IsDelete != true); // Исключаем удаленные предметы

            // Если указан идентификатор учителя, фильтруем по нему
            if (teacherId.HasValue)
            {
                query = query.Where(s => s.IdEmployees.Any(e => e.IdEmployee == teacherId));
            }

            // Получаем список предметов с необходимыми данными
            var subjects = await query
                .Select(s => new SubjectListItemDto
                {
                    IdSubject = s.IdSubject,
                    Name = s.Name,
                    FullName = s.FullName,
                    //GroupsCount = s.Groups.Count(g => g.IsDelete != true), // Подсчет активных групп
                })
                .ToListAsync();

            return Ok(subjects);
        }

        /// <summary>
        /// Получает группы, связанные с указанным предметом.
        /// </summary>
        /// <param name="subjectId">Идентификатор предмета.</param>
        /// <param name="teacherId">Идентификатор учителя (необязательный).</param>
        /// <returns>Список групп по предмету.</returns>
        [HttpGet("{subjectId}/Groups")]
        public async Task<ActionResult<IEnumerable<GroupDto>>> GetSubjectGroups(
            int subjectId,
            [FromQuery] int? teacherId)
        {
            // Проверка существования предмета
            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.IdSubject == subjectId && s.IsDelete != true);

            if (!subjectExists) return NotFound("Предмет не найден");

            // Формируем запрос для получения групп по предмету
            var query = _context.Groups
                .Where(g => g.IdSubject == subjectId && g.IsDelete != true); // Исключаем удаленные группы

            // Если указан идентификатор учителя, фильтруем по нему
            if (teacherId.HasValue)
            {
                query = query.Where(g => g.IdEmployees.Any(e => e.IdEmployee == teacherId));
            }

            // Получаем список групп с необходимыми данными
            var groups = await query
                .Select(g => new GroupDto
                {
                    IdGroup = g.IdGroup,
                    Name = g.Name,
                })
                .ToListAsync();

            return Ok(groups);
        }

        /// <summary>
        /// Получает журналы для указанной группы.
        /// </summary>
        /// <param name="groupId">Идентификатор группы.</param>
        /// <returns>Список журналов группы.</returns>
        [HttpGet("Groups/{groupId}/Journals")]
        public async Task<ActionResult<IEnumerable<JournalDto>>> GetGroupJournals(int groupId)
        {
            // Проверка существования группы
            var group = await _context.Groups
                .Include(g => g.Journals) // Включаем журналы группы
                .FirstOrDefaultAsync(g => g.IdGroup == groupId && g.IsDelete != true);

            if (group == null) return NotFound
                ("Группа не найдена");

            // Получаем список журналов группы с необходимыми данными
            var journals = group.Journals
                .Select(j => new JournalDto
                {
                    IdJournal = j.IdJournal,
                    StartDate = j.StartDate,
                    EndDate = j.EndDate,
                    Name = $"{j.StartDate:yyyy} - {j.EndDate:yyyy}" // Форматируем название журнала
                })
                .OrderByDescending(j => j.StartDate) // Сортируем по дате начала
                .ToList();

            return Ok(journals);
        }

        /// <summary>
        /// Получает уроки для указанного журнала.
        /// </summary>
        /// <param name="journalId">Идентификатор журнала.</param>
        /// <returns>Список уроков журнала.</returns>
        [HttpGet("Journals/{journalId}/Lessons")]
        public async Task<ActionResult<JournalLessonsResponse>> GetJournalLessons(int journalId)
        {
            // Проверяем существование журнала
            var journal = await _context.Journals
                .Include(j => j.Lessons)
                    .ThenInclude(l => l.Visits)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                .FirstOrDefaultAsync(j => j.IdJournal == journalId);

            if (journal == null) return NotFound("Журнал не найден");

            // Получаем список студентов группы
            var students = journal.IdGroupNavigation.IdStudents.ToList();

            // Создаем отсутствующие записи посещений
            foreach (var lesson in journal.Lessons)
            {
                foreach (var student in students)
                {
                    // Проверяем, есть ли уже запись о посещении
                    if (!lesson.Visits.Any(v => v.IdStudent == student.IdStudent))
                    {
                        var newVisit = new Visit
                        {
                            IdStudent = student.IdStudent,
                            IdLesson = lesson.IdLesson,
                            UnvisitedStatuses = null,  // По умолчанию - присутствовал
                            Comment = null
                        };
                        _context.Visits.Add(newVisit);
                    }
                }
            }

            // Сохраняем изменения
            await _context.SaveChangesAsync();

            // Перезагружаем данные после изменений
            journal = await _context.Journals
                .Include(j => j.Lessons)
                    .ThenInclude(l => l.Visits)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                .FirstOrDefaultAsync(j => j.IdJournal == journalId);

            // Формируем список студентов
            var studentDtos = journal.IdGroupNavigation.IdStudents
                .Select(s => new StudentVisitDto
                {
                    IdStudent = s.IdStudent,
                    FullName = $"{s.Surname} {s.Name} {s.Patronymic}".Trim()
                }).ToList();

            // Формируем список уроков с посещениями
            var lessonDtos = journal.Lessons
                .Select(l => new LessonDto
                {
                    IdLesson = l.IdLesson,
                    LessonDate = l.LessonDate,
                    Visits = l.Visits.ToDictionary(
                        v => v.IdStudent,
                        v => new VisitDto
                        {
                            IdVisit = v.IdVisit,
                            UnvisitedStatuses = v.UnvisitedStatuses,
                            Comment = v.Comment
                        })
                }).ToList();

            return Ok(new JournalLessonsResponse
            {
                Students = studentDtos,
                Lessons = lessonDtos,
                JournalInfo = new JournalInfoDto
                {
                    IdJournal = journal.IdJournal,
                    StartDate = journal.StartDate,
                    EndDate = journal.EndDate,
                    LessonsCount = journal.LessonsCount
                }
            });
        }

        /// <summary>
        /// Создает новый журнал для указанной группы.
        /// </summary>
        /// <param name="dto">Данные для создания журнала.</param>
        /// <returns>Созданный журнал.</returns>
        [HttpPost("Journals")]
        public async Task<ActionResult<JournalDto>> CreateJournal([FromBody] CreateJournalDto dto)
        {
            // Проверка обязательных полей
            if (dto.GroupId <= 0)
                return BadRequest("Неверный идентификатор группы");

            if (dto.LessonsCount == null || dto.LessonsCount < 1)
                return BadRequest("Количество уроков должно быть положительным числом");

            if (dto.StartDate >= dto.EndDate)
                return BadRequest("Дата окончания должна быть позже даты начала");

            // Проверка существования группы
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.IdGroup == dto.GroupId && g.IsDelete != true);
            if (group == null) return BadRequest("Группа не найдена");

            // Проверка на пересечение дат
            var existingJournal = await _context.Journals
                .Where(j => j.IdGroup == dto.GroupId &&
                    (j.StartDate <= dto.EndDate && j.EndDate >= dto.StartDate))
                .FirstOrDefaultAsync();

            if (existingJournal != null)
                return BadRequest("Журнал на этот период уже существует");

            // Создание нового журнала
            var journal = new Journal
            {
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IdGroup = dto.GroupId,
                LessonsCount = dto.LessonsCount
            };

            _context.Journals.Add(journal);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGroupJournals), new { groupId = dto.GroupId },
                new JournalDto
                {
                    IdJournal = journal.IdJournal,
                    StartDate = journal.StartDate,
                    EndDate = journal.EndDate,
                    Name = $"{journal.StartDate:yyyy} - {journal.EndDate:yyyy}"
                });
        }

        /// <summary>
        /// Добавляет новый урок в указанный журнал.
        /// </summary>
        /// <param name="journalId">Идентификатор журнала.</param>
        /// <param name="dateString">Дата урока в формате строки (YYYY-MM-DD).</param>
        /// <returns>Добавленный урок.</returns>
        [HttpPost("Journals/{journalId}/Lessons")]
        public async Task<ActionResult<LessonDto>> AddLesson(
            int journalId,
            [FromBody] string dateString)
        {
            if (!DateOnly.TryParse(dateString, out var lessonDate))
            {
                return BadRequest("Некорректный формат даты. Используйте формат YYYY-MM-DD");
            }

            // Проверка существования журнала
            var journal = await _context.Journals
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                .FirstOrDefaultAsync(j => j.IdJournal == journalId);

            if (journal == null)
                return NotFound("Журнал не найден");

            // Проверка на существующую дату
            if (await _context.Lessons.AnyAsync(l => l.IdJournal == journalId && l.LessonDate == lessonDate))
                return BadRequest("Урок на эту дату уже существует");

            // Проверка что дата в пределах периода журнала
            if (lessonDate < journal.StartDate || lessonDate > journal.EndDate)
                return BadRequest($"Дата урока должна быть между {journal.StartDate:yyyy-MM-dd} и {journal.EndDate:yyyy-MM-dd}");

            // Создание нового урока
            var lesson = new Lesson
            {
                LessonDate = lessonDate,
                IdJournal = journalId
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            // Создаем посещения для всех студентов группы
            foreach (var student in journal.IdGroupNavigation.IdStudents)
            {
                _context.Visits.Add(new Visit
                {
                    IdLesson = lesson.IdLesson,
                    IdStudent = student.IdStudent,
                    UnvisitedStatuses = null,
                    Comment = null
                });
            }

            await _context.SaveChangesAsync();

            // Формируем ответ
            var visits = journal.IdGroupNavigation.IdStudents
                .ToDictionary(
                    s => s.IdStudent,
                    s => new VisitDto
                    {
                        IdVisit = _context.Visits
                            .First(v => v.IdLesson == lesson.IdLesson &&
                                        v.IdStudent == s.IdStudent).IdVisit,
                        UnvisitedStatuses = null,
                        Comment = null
                    });

            return Ok(new LessonDto
            {
                IdLesson = lesson.IdLesson,
                LessonDate = lesson.LessonDate,
                Visits = visits
            });
        }

        /// <summary>
        /// Обновляет комментарий для указанного посещения.
        /// </summary>
        /// <param name="visitId">Идентификатор посещения.</param>
        /// <param name="dto">Данные для обновления комментария.</param>
        /// <returns>Результат операции.</returns>
        /// <summary>
        /// Обновляет статус посещения
        /// </summary>
        [HttpPut("Visits/{visitId}")]
        public async Task<IActionResult> UpdateVisit(int visitId, [FromBody] UpdateVisitDto dto)
        {
            var visit = await _context.Visits.FindAsync(visitId);
            if (visit == null) return NotFound("Посещение не найдено");

            visit.UnvisitedStatuses = dto.UnvisitedStatuses;
            visit.Comment = dto.Comment;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                statusClass = visit.UnvisitedStatuses switch
                {
                    "н" => "table-danger",
                    "б" => "table-warning",
                    "у/п" => "table-info",
                    "к" => "table-secondary",
                    _ => "table-success"
                }
            });
        }

        /// <summary>
        /// Добавляет новое посещение для указанного студента и урока.
        /// </summary>
        /// <param name="dto">Данные для создания посещения.</param>
        /// <returns>Добавленное посещение.</returns>
        [HttpPost("Visits")]
        public async Task<ActionResult<VisitDto>> AddVisit([FromBody] CreateVisitDto dto)
        {
            // Проверка существования студента и урока
            var student = await _context.Students.FindAsync(dto.StudentId);
            var lesson = await _context.Lessons.FindAsync(dto.LessonId);

            if (student == null || lesson == null)
                return BadRequest("Студент или урок не найден");

            // Проверка на существующее посещение
            var existingVisit = await _context.Visits
                .FirstOrDefaultAsync(v => v.IdStudent == dto.StudentId &&
                                       v.IdLesson == dto.LessonId);

            if (existingVisit != null)
                return Conflict("Посещение уже существует");

            // Создание нового посещения
            var visit = new Visit
            {
                IdStudent = dto.StudentId,
                IdLesson = dto.LessonId,
                UnvisitedStatuses = null,
                Comment = null
            };

            _context.Visits.Add(visit);
            await _context.SaveChangesAsync();

            return Ok(new VisitDto
            {
                IdVisit = visit.IdVisit,
                UnvisitedStatuses = visit.UnvisitedStatuses,
                Comment = visit.Comment
            });
        }

        [HttpGet("Groups/{groupId}/CurrentJournal")]
        public async Task<ActionResult<JournalLessonsResponse>> GetCurrentGroupJournal(int groupId)
        {
            // Проверяем существование группы
            var groupExists = await _context.Groups
                .AnyAsync(g => g.IdGroup == groupId && g.IsDelete != true);

            if (!groupExists)
                return NotFound("Группа не найдена");

            // Находим последний журнал группы
            var journal = await _context.Journals
                .Include(j => j.Lessons)
                    .ThenInclude(l => l.Visits)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                .Where(j => j.IdGroup == groupId)
                .OrderByDescending(j => j.StartDate)
                .FirstOrDefaultAsync();

            if (journal == null)
            {
                // Возвращаем пустой ответ с сообщением
                return Ok(new JournalLessonsResponse
                {
                    Message = "Для этой группы еще не создан журнал",
                    Students = new List<StudentVisitDto>(),
                    Lessons = new List<LessonDto>()
                });
            }

            // Получаем список студентов
            var students = journal.IdGroupNavigation.IdStudents.ToList();

            // Создаем отсутствующие записи посещений
            foreach (var lesson in journal.Lessons)
            {
                foreach (var student in students)
                {
                    if (!lesson.Visits.Any(v => v.IdStudent == student.IdStudent))
                    {
                        var newVisit = new Visit
                        {
                            IdStudent = student.IdStudent,
                            IdLesson = lesson.IdLesson,
                            UnvisitedStatuses = null,
                            Comment = null
                        };
                        _context.Visits.Add(newVisit);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Перезагружаем данные
            journal = await _context.Journals
                .Include(j => j.Lessons)
                    .ThenInclude(l => l.Visits)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                .Where(j => j.IdGroup == groupId)
                .OrderByDescending(j => j.StartDate)
                .FirstOrDefaultAsync();

            // Формируем ответ
            var studentDtos = journal.IdGroupNavigation.IdStudents
                .Select(s => new StudentVisitDto
                {
                    IdStudent = s.IdStudent,
                    FullName = $"{s.Surname} {s.Name} {s.Patronymic}".Trim()
                }).ToList();

            var lessonDtos = journal.Lessons
                .Select(l => new LessonDto
                {
                    IdLesson = l.IdLesson,
                    LessonDate = l.LessonDate,
                    Visits = l.Visits.ToDictionary(
                        v => v.IdStudent,
                        v => new VisitDto
                        {
                            IdVisit = v.IdVisit,
                            UnvisitedStatuses = v.UnvisitedStatuses,
                            Comment = v.Comment
                        })
                }).ToList();

            return Ok(new JournalLessonsResponse
            {
                Students = studentDtos,
                Lessons = lessonDtos
            });
        }

        [HttpGet("Groups/{groupId}/Students")]
        public async Task<ActionResult<IEnumerable<StudentVisitDto>>> GetGroupStudents(int groupId)
        {
            var group = await _context.Groups
                .Include(g => g.IdStudents)
                .FirstOrDefaultAsync(g => g.IdGroup == groupId && g.IsDelete != true);

            if (group == null) return NotFound("Группа не найдена");

            var students = group.IdStudents
                .Select(s => new StudentVisitDto
                {
                    IdStudent = s.IdStudent,
                    FullName = $"{s.Surname} {s.Name} {s.Patronymic}".Trim()
                }).ToList();

            return Ok(students);
        }
    }
}