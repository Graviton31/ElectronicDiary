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

        [HttpGet("Subjects")]
        public async Task<ActionResult<IEnumerable<SubjectListItemDto>>> GetSubjects(
            [FromQuery] int? teacherId)
        {
            var query = _context.Subjects
                .Include(s => s.IdEmployees)
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
                })
                .ToListAsync();

            return Ok(subjects);
        }

        [HttpGet("{subjectId}/Groups")]
        public async Task<ActionResult<IEnumerable<GroupDto>>> GetSubjectGroups(
            int subjectId,
            [FromQuery] int? teacherId)
        {
            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.IdSubject == subjectId && s.IsDelete != true);

            if (!subjectExists) return NotFound("Предмет не найден");

            var query = _context.Groups
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
                })
                .ToListAsync();

            return Ok(groups);
        }

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

        [HttpGet("Journals/{journalId}/Lessons")]
        public async Task<ActionResult<JournalLessonsResponse>> GetJournalLessons(int journalId)
        {
            var journal = await _context.Journals
                .Include(j => j.Lessons)
                    .ThenInclude(l => l.Visits)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                        .ThenInclude(s => s.IdStudentNavigation)
                .FirstOrDefaultAsync(j => j.IdJournal == journalId);

            if (journal == null) return NotFound("Журнал не найден");

            var students = journal.IdGroupNavigation.IdStudents.ToList();

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

            journal = await _context.Journals
                .Include(j => j.Lessons)
                    .ThenInclude(l => l.Visits)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                        .ThenInclude(s => s.IdStudentNavigation)
                .FirstOrDefaultAsync(j => j.IdJournal == journalId);

            var studentDtos = journal.IdGroupNavigation.IdStudents
                .Select(s => new StudentVisitDto
                {
                    IdStudent = s.IdStudent,
                    FullName = $"{s.IdStudentNavigation.Surname} {s.IdStudentNavigation.Name} {s.IdStudentNavigation.Patronymic}".Trim()
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

            var absentCount = journal.Lessons
                .SelectMany(l => l.Visits)
                .Count(v => v.UnvisitedStatuses == "н");

            return Ok(new JournalLessonsResponse
            {
                Students = studentDtos,
                Lessons = lessonDtos,
                JournalInfo = new JournalInfoDto
                {
                    IdJournal = journal.IdJournal,
                    StartDate = journal.StartDate,
                    EndDate = journal.EndDate,
                    LessonsCount = journal.LessonsCount,
                    CompletedLessons = journal.Lessons.Count,
                    AbsentCount = absentCount
                }
            });
        }

        [HttpPost("Journals")]
        public async Task<ActionResult<JournalDto>> CreateJournal([FromBody] CreateJournalDto dto)
        {
            if (dto.GroupId <= 0)
                return BadRequest("Неверный идентификатор группы");

            if (dto.LessonsCount == null || dto.LessonsCount < 1)
                return BadRequest("Количество уроков должно быть положительным числом");

            if (dto.StartDate >= dto.EndDate)
                return BadRequest("Дата окончания должна быть позже даты начала");

            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.IdGroup == dto.GroupId && g.IsDelete != true);
            if (group == null) return BadRequest("Группа не найдена");

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

        [HttpPost("Journals/{journalId}/Lessons")]
        public async Task<ActionResult<LessonDto>> AddLesson(
            int journalId,
            [FromBody] string dateString)
        {
            if (!DateOnly.TryParse(dateString, out var lessonDate))
            {
                return BadRequest("Некорректный формат даты. Используйте формат YYYY-MM-DD");
            }

            var journal = await _context.Journals
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                .FirstOrDefaultAsync(j => j.IdJournal == journalId);

            if (journal == null)
                return NotFound("Журнал не найден");

            if (await _context.Lessons.AnyAsync(l => l.IdJournal == journalId && l.LessonDate == lessonDate))
                return BadRequest("Урок на эту дату уже существует");

            if (lessonDate < journal.StartDate || lessonDate > journal.EndDate)
                return BadRequest($"Дата урока должна быть между {journal.StartDate:yyyy-MM-dd} и {journal.EndDate:yyyy-MM-dd}");

            var lesson = new Lesson
            {
                LessonDate = lessonDate,
                IdJournal = journalId
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

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

        [HttpPost("Visits")]
        public async Task<ActionResult<VisitDto>> AddVisit([FromBody] CreateVisitDto dto)
        {
            var student = await _context.Students.FindAsync(dto.StudentId);
            var lesson = await _context.Lessons.FindAsync(dto.LessonId);

            if (student == null || lesson == null)
                return BadRequest("Студент или урок не найден");

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

        [HttpGet("Groups/{groupId}/CurrentJournal")]
        public async Task<ActionResult<JournalLessonsResponse>> GetCurrentGroupJournal(int groupId)
        {
            var groupExists = await _context.Groups
                .AnyAsync(g => g.IdGroup == groupId && g.IsDelete != true);

            if (!groupExists)
                return NotFound("Группа не найдена");

            var journal = await _context.Journals
                .Include(j => j.Lessons)
                    .ThenInclude(l => l.Visits)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                        .ThenInclude(s => s.IdStudentNavigation)
                .Where(j => j.IdGroup == groupId)
                .OrderByDescending(j => j.StartDate)
                .FirstOrDefaultAsync();

            if (journal == null)
            {
                return Ok(new JournalLessonsResponse
                {
                    Message = "Для этой группы еще не создан журнал",
                    Students = new List<StudentVisitDto>(),
                    Lessons = new List<LessonDto>()
                });
            }

            var students = journal.IdGroupNavigation.IdStudents.ToList();

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

            journal = await _context.Journals
                .Include(j => j.Lessons)
                    .ThenInclude(l => l.Visits)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                        .ThenInclude(s => s.IdStudentNavigation)
                .Where(j => j.IdGroup == groupId)
                .OrderByDescending(j => j.StartDate)
                .FirstOrDefaultAsync();

            var studentDtos = journal.IdGroupNavigation.IdStudents
                .Select(s => new StudentVisitDto
                {
                    IdStudent = s.IdStudent,
                    FullName = $"{s.IdStudentNavigation.Surname} {s.IdStudentNavigation.Name} {s.IdStudentNavigation.Patronymic}".Trim()
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
                    .ThenInclude(s => s.IdStudentNavigation)
                .FirstOrDefaultAsync(g => g.IdGroup == groupId && g.IsDelete != true);

            if (group == null) return NotFound("Группа не найдена");

            var students = group.IdStudents
                .Select(s => new StudentVisitDto
                {
                    IdStudent = s.IdStudent,
                    FullName = $"{s.IdStudentNavigation.Surname} {s.IdStudentNavigation.Name} {s.IdStudentNavigation.Patronymic}".Trim()
                }).ToList();

            return Ok(students);
        }
    }
}