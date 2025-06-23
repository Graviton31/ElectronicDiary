using ClosedXML.Excel;
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

        [HttpGet("Journals/{journalId}/ExportExcel")]
        public async Task<IActionResult> ExportJournalToExcel(int journalId)
        {
            var journal = await _context.Journals
                .Include(j => j.Lessons)
                    .ThenInclude(l => l.Visits)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdStudents)
                        .ThenInclude(s => s.IdStudentNavigation)
                .Include(j => j.IdGroupNavigation)
                    .ThenInclude(g => g.IdSubjectNavigation)
                .FirstOrDefaultAsync(j => j.IdJournal == journalId);

            if (journal == null)
                return NotFound("Журнал не найден");

            // Формируем данные для экспорта
            var exportData = new JournalExcelExportDto
            {
                GroupName = journal.IdGroupNavigation.Name,
                SubjectName = journal.IdGroupNavigation.IdSubjectNavigation.Name,
                StartDate = journal.StartDate ?? DateOnly.MinValue,
                EndDate = journal.EndDate ?? DateOnly.MinValue,
                Students = journal.IdGroupNavigation.IdStudents
                    .Select(s => new StudentVisitDto
                    {
                        IdStudent = s.IdStudent,
                        FullName = $"{s.IdStudentNavigation.Surname} {s.IdStudentNavigation.Name} {s.IdStudentNavigation.Patronymic}".Trim()
                    })
                    .ToList(),
                Lessons = journal.Lessons
                    .OrderBy(l => l.LessonDate)
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
                    })
                    .ToList()
            };

            // Генерируем Excel и возвращаем файл
            return GenerateExcelFile(exportData);
        }

        private IActionResult GenerateExcelFile(JournalExcelExportDto data)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Журнал посещений");

                // Стили границ
                var borderStyle = XLBorderStyleValues.Medium;
                var borderColor = XLColor.FromArgb(200, 200, 200); // Серый

                // Стили
                var headerStyle = workbook.Style;
                headerStyle.Font.Bold = true;
                headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerStyle.Fill.BackgroundColor = XLColor.FromArgb(234, 244, 249); // Мягкий голубой
                headerStyle.Font.FontColor = XLColor.FromArgb(51, 51, 51); // Темно-серый
                headerStyle.Border.TopBorder = borderStyle;
                headerStyle.Border.BottomBorder = borderStyle;
                headerStyle.Border.LeftBorder = borderStyle;
                headerStyle.Border.RightBorder = borderStyle;
                headerStyle.Border.TopBorderColor = borderColor;
                headerStyle.Border.BottomBorderColor = borderColor;
                headerStyle.Border.LeftBorderColor = borderColor;
                headerStyle.Border.RightBorderColor = borderColor;

                var totalStyle = workbook.Style;
                totalStyle.Font.Bold = true;
                totalStyle.Fill.BackgroundColor = XLColor.FromArgb(245, 245, 245); // Светло-серый
                totalStyle.Font.FontColor = XLColor.FromArgb(51, 51, 51);
                totalStyle.Border.TopBorder = borderStyle;
                totalStyle.Border.BottomBorder = borderStyle;
                totalStyle.Border.LeftBorder = borderStyle;
                totalStyle.Border.RightBorder = borderStyle;
                totalStyle.Border.TopBorderColor = borderColor;
                totalStyle.Border.BottomBorderColor = borderColor;
                totalStyle.Border.LeftBorderColor = borderColor;
                totalStyle.Border.RightBorderColor = borderColor;

                var defaultCellStyle = workbook.Style;
                defaultCellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                defaultCellStyle.Font.FontColor = XLColor.FromArgb(68, 68, 68);
                defaultCellStyle.Border.TopBorder = borderStyle;
                defaultCellStyle.Border.BottomBorder = borderStyle;
                defaultCellStyle.Border.LeftBorder = borderStyle;
                defaultCellStyle.Border.RightBorder = borderStyle;
                defaultCellStyle.Border.TopBorderColor = borderColor;
                defaultCellStyle.Border.BottomBorderColor = borderColor;
                defaultCellStyle.Border.LeftBorderColor = borderColor;
                defaultCellStyle.Border.RightBorderColor = borderColor;

                // Заголовок (название группы, предмета, период)
                worksheet.Cell(1, 1).Value = $"Группа: {data.GroupName}";
                worksheet.Cell(2, 1).Value = $"Предмет: {data.SubjectName}";
                worksheet.Cell(3, 1).Value = $"Период: {data.StartDate:dd.MM.yyyy} - {data.EndDate:dd.MM.yyyy}";
                worksheet.Range(1, 1, 3, 1).Style.Font.FontColor = XLColor.FromArgb(51, 51, 51);

                // Заголовки таблицы
                int currentRow = 5;
                worksheet.Cell(currentRow, 1).Value = "№";
                worksheet.Cell(currentRow, 2).Value = "ФИО студента";

                // Заполняем даты занятий (без года)
                for (int i = 0; i < data.Lessons.Count; i++)
                {
                    worksheet.Cell(currentRow, 3 + i).Value = data.Lessons[i].LessonDate?.ToString("dd.MM") ?? "Дата";
                }

                // Добавляем колонки для статистики
                int statsStartCol = 3 + data.Lessons.Count;
                worksheet.Cell(currentRow, statsStartCol).Value = "Пропуски (н)";
                worksheet.Cell(currentRow, statsStartCol + 1).Value = "Ув. причина";
                worksheet.Cell(currentRow, statsStartCol + 2).Value = "Всего";

                // Применяем стиль заголовков
                worksheet.Range(currentRow, 1, currentRow, statsStartCol + 2).Style = headerStyle;

                // Данные студентов
                for (int i = 0; i < data.Students.Count; i++)
                {
                    var student = data.Students[i];
                    currentRow++;

                    worksheet.Cell(currentRow, 1).Value = i + 1;
                    worksheet.Cell(currentRow, 2).Value = student.FullName;
                    worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    // Заполняем посещения
                    for (int j = 0; j < data.Lessons.Count; j++)
                    {
                        var lesson = data.Lessons[j];
                        var visit = lesson.Visits.GetValueOrDefault(student.IdStudent);
                        var cell = worksheet.Cell(currentRow, 3 + j);

                        cell.Value = visit?.UnvisitedStatuses ?? "✓";
                        cell.Style = defaultCellStyle;

                        // Прозрачные стили для статусов
                        switch (visit?.UnvisitedStatuses)
                        {
                            case "н":
                                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 230, 230);
                                cell.Style.Font.FontColor = XLColor.FromArgb(204, 0, 0);
                                break;
                            case "у/п":
                                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(230, 240, 255);
                                cell.Style.Font.FontColor = XLColor.FromArgb(0, 92, 204);
                                break;
                            case "к":
                                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(240, 240, 240);
                                cell.Style.Font.FontColor = XLColor.FromArgb(102, 102, 102);
                                break;
                            case "б":
                                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 230);
                                cell.Style.Font.FontColor = XLColor.FromArgb(153, 102, 0);
                                break;
                            default:
                                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(230, 255, 230);
                                cell.Style.Font.FontColor = XLColor.FromArgb(0, 102, 0);
                                break;
                        }
                    }

                    // Добавляем формулы для подсчёта статистики
                    var firstLessonCell = worksheet.Cell(currentRow, 3);
                    var lastLessonCell = worksheet.Cell(currentRow, 2 + data.Lessons.Count);
                    string lessonRange = $"{firstLessonCell.Address}:{lastLessonCell.Address}";

                    worksheet.Cell(currentRow, statsStartCol).FormulaA1 = $"COUNTIF({lessonRange}, \"н\")";
                    worksheet.Cell(currentRow, statsStartCol + 1).FormulaA1 =
                        $"COUNTIF({lessonRange}, \"у/п\") + COUNTIF({lessonRange}, \"к\") + COUNTIF({lessonRange}, \"б\")";
                    worksheet.Cell(currentRow, statsStartCol + 2).FormulaA1 =
                        $"{worksheet.Cell(currentRow, statsStartCol).Address} + {worksheet.Cell(currentRow, statsStartCol + 1).Address}";
                }

                // Добавляем итоговую строку
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = "Итого:";
                worksheet.Range(currentRow, 1, currentRow, 2).Merge();
                worksheet.Range(currentRow, 1, currentRow, 2).Style = totalStyle;

                // Итоговые формулы
                for (int i = 0; i < 3; i++)
                {
                    var col = statsStartCol + i;
                    var firstCell = worksheet.Cell(6, col);
                    var lastCell = worksheet.Cell(currentRow - 1, col);
                    worksheet.Cell(currentRow, col).FormulaA1 = $"SUM({firstCell.Address}:{lastCell.Address})";
                    worksheet.Cell(currentRow, col).Style = totalStyle;
                }

                // Автонастройка ширины столбцов
                worksheet.Columns().AdjustToContents();

                // Устанавливаем минимальную ширину для колонок
                for (int i = 3; i < 3 + data.Lessons.Count; i++)
                {
                    worksheet.Column(i).Width = Math.Max(worksheet.Column(i).Width, 6);
                }
                worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 20); // Ширина для ФИО

                // Сохраняем в MemoryStream и возвращаем файл
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Журнал_{data.GroupName}_{DateTime.Now:yyyyMMdd}.xlsx"
                    );
                }
            }
        }
    }
}