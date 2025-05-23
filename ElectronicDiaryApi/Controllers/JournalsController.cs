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

        // 1. Получение списка предметов
        [HttpGet("Subjects")]
        public async Task<ActionResult<IEnumerable<SubjectListItemDto>>> GetSubjects(
            [FromQuery] int? teacherId)
        {
            var query = _context.Subjects
                .Include(s => s.IdEmployees)
                .Include(s => s.Groups)
                .Where(s => s.IsDelete != true);

            if (teacherId.HasValue)
            {
                query = query.Where(s => s.IdEmployees.Any(e => e.IdEmployee == teacherId));
            }

            var subjects = await query
                .Select(s => new SubjectListItemDto
                {
                    IdSubject = s.IdSubject,
                    Name = s.Name,
                    FullName = s.FullName,
                    GroupsCount = s.Groups.Count(g => g.IsDelete != true),
                    TeachersCount = s.IdEmployees.Count
                })
                .ToListAsync();

            return Ok(subjects);
        }

        // 2. Получение групп по предмету
        [HttpGet("{subjectId}/Groups")]
        public async Task<ActionResult<IEnumerable<GroupDto>>> GetSubjectGroups(
            int subjectId,
            [FromQuery] int? teacherId)
        {
            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.IdSubject == subjectId && s.IsDelete != true);

            if (!subjectExists) return NotFound("Предмет не найден");

            var query = _context.Groups
                .Include(g => g.IdLocationNavigation)
                .Include(g => g.IdEmployees)
                .Include(g => g.IdSubjectNavigation)
                .Where(g => g.IdSubject == subjectId && g.IsDelete != true);

            if (teacherId.HasValue)
            {
                query = query.Where(g => g.IdEmployees.Any(e => e.IdEmployee == teacherId));
            }

            var groups = await query
                .Select(g => new GroupDto
                {
                    IdGroup = g.IdGroup,
                    Name = g.Name,
                    MinAge = g.MinAge,
                    MaxAge = g.MaxAge,
                    Location = new LocationDto
                    {
                        IdLocation = g.IdLocationNavigation.IdLocation,
                        Name = g.IdLocationNavigation.Name,
                        Address = g.IdLocationNavigation.Addres
                    },
                    Teachers = g.IdEmployees.Select(e => new EmployeeDto
                    {
                        IdEmployee = e.IdEmployee,
                        FullName = $"{e.Surname} {e.Name} {e.Patronymic}".Trim()
                    }).ToList(),
                    MaxStudentCount = g.MaxStudentCount,
                    CurrentStudents = g.IdStudents.Count,
                    IdSubject = g.IdSubject,
                    SubjectName = g.IdSubjectNavigation.Name
                })
                .ToListAsync();

            return Ok(groups);
        }

        // 3. Получение журналов группы
        [HttpGet("Groups/{groupId}/Journals")]
        public async Task<ActionResult<IEnumerable<JournalDto>>> GetGroupJournals(int groupId)
        {
            var group = await _context.Groups
                .Include(g => g.Journals)
                .FirstOrDefaultAsync(g => g.IdGroup == groupId && g.IsDelete != true);

            if (group == null) return NotFound("Группа не найдена");

            var journals = group.Journals
                .Select(j => new JournalDto
                {
                    IdJournal = j.IdJournal,
                    StartDate = j.StartDate,
                    EndDate = j.EndDate,
                    Name = $"{j.StartDate:yyyy} - {j.EndDate:yyyy}"
                })
                .OrderByDescending(j => j.StartDate)
                .ToList();

            return Ok(journals);
        }

        // 4. Получение уроков журнала
        [HttpGet("Journals/{journalId}/Lessons")]
        public async Task<ActionResult<JournalLessonsResponse>> GetJournalLessons(int journalId)
        {
            var journal = await _context.Journals
                .Include(j => j.Lessons)
                    .ThenInclude(l => l.Visits)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                .FirstOrDefaultAsync(j => j.IdJournal == journalId);

            if (journal == null) return NotFound("Журнал не найден");

            var students = journal.IdGroupNavigation.IdStudents
                .Select(s => new StudentVisitDto
                {
                    IdStudent = s.IdStudent,
                    FullName = $"{s.Surname} {s.Name} {s.Patronymic}".Trim()
                }).ToList();

            var lessons = journal.Lessons
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
                Students = students,
                Lessons = lessons
            });
        }

        [HttpPost("Journals")]
        public async Task<ActionResult<JournalDto>> CreateJournal([FromBody] CreateJournalDto dto)
        {
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

            var journal = new Journal
            {
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IdGroup = dto.GroupId,
                LessonsCount = 0 // Инициализация, будет обновляться при добавлении уроков
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

        // 5. Добавление урока
        [HttpPost("Journals/{journalId}/Lessons")]
        public async Task<ActionResult<LessonDto>> AddLesson(
            int journalId,
            [FromBody] DateOnly lessonDate)
        {
            var journal = await _context.Journals
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                .FirstOrDefaultAsync(j => j.IdJournal == journalId);

            if (journal == null) return NotFound("Журнал не найден");

            // Проверка на существующую дату
            if (journal.Lessons.Any(l => l.LessonDate == lessonDate))
                return BadRequest("Урок на эту дату уже существует");

            var lesson = new Lesson
            {
                LessonDate = lessonDate,
                IdJournal = journalId
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            // Создаем посещения для всех студентов
            foreach (var student in journal.IdGroupNavigation.IdStudents)
            {
                _context.Visits.Add(new Visit
                {
                    IdLesson = lesson.IdLesson,
                    IdStudent = student.IdStudent
                });
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetJournalLessons),
                new { journalId },
                new LessonDto
                {
                    IdLesson = lesson.IdLesson,
                    LessonDate = lesson.LessonDate,
                    Visits = journal.IdGroupNavigation.IdStudents.ToDictionary(
                        s => s.IdStudent,
                        s => new VisitDto
                        {
                            IdVisit = _context.Visits
                                .First(v => v.IdLesson == lesson.IdLesson &&
                                       v.IdStudent == s.IdStudent).IdVisit
                        })
                });
        }

        [HttpPatch("Visits/{visitId}/Status")]
        public async Task<IActionResult> UpdateVisitStatus(
            int visitId,
            [FromBody] UpdateVisitStatusDto dto)
        {
            var visit = await _context.Visits.FindAsync(visitId);
            if (visit == null) return NotFound("Посещение не найдено");

            visit.UnvisitedStatuses = dto.UnvisitedStatuses;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("Visits/{visitId}/Comment")]
        public async Task<IActionResult> UpdateVisitComment(
            int visitId,
            [FromBody] UpdateVisitCommentDto dto)
        {
            var visit = await _context.Visits.FindAsync(visitId);
            if (visit == null) return NotFound("Посещение не найдено");

            visit.Comment = dto.Comment;
            await _context.SaveChangesAsync();

            return NoContent();
        }

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
    }
}