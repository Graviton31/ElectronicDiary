using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto.Responses;
using ElectronicDiaryApi.ModelsDto.UsersView;
using ElectronicDiaryApi.ModelsDto.EnrollmentRequest;
using ElectronicDiaryApi.ModelsDto.Subject;
using ElectronicDiaryApi.ModelsDto.Group;

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;
        private readonly ILogger<ElectronicDiaryContext> _logger;

        public StudentsController(ElectronicDiaryContext context, ILogger<ElectronicDiaryContext> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Students
        [HttpGet]
        public async Task<ActionResult<PagedResponse<StudentDto>>> GetStudents(
            int page = 1,
            int pageSize = 10,
            string search = "")
        {
            try
            {
                var query = _context.Students
                    .Include(s => s.IdStudentNavigation)
                    .Where(s => s.IdStudentNavigation.IsDelete != true)
                    .AsQueryable();

                // Добавляем поиск
                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.ToLower();
                    query = query.Where(s =>
                        (s.IdStudentNavigation.Surname + " " +
                         s.IdStudentNavigation.Name + " " +
                         s.IdStudentNavigation.Patronymic).ToLower().Contains(searchTerm) ||
                        s.IdStudentNavigation.Login.ToLower().Contains(searchTerm) ||
                        (s.EducationName != null && s.EducationName.ToLower().Contains(searchTerm))
                    );
                }

                var totalCount = await query.CountAsync();
                var data = await query
                    .Select(s => new StudentDto
                    {
                        IdStudent = s.IdStudent,
                        FullName = s.IdStudentNavigation.Surname + " " +
                                  s.IdStudentNavigation.Name + " " +
                                  s.IdStudentNavigation.Patronymic,
                        Login = s.IdStudentNavigation.Login,
                        Phone = s.IdStudentNavigation.Phone,
                        EducationName = s.EducationName
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new PagedResponse<StudentDto>
                {
                    Data = data,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
        }

        [HttpGet("Details/{id}")]
        public async Task<ActionResult<StudentDto>> GetStudentDetails(int id)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.IdStudentNavigation) // Добавляем загрузку пользователя
                    .Include(s => s.EnrollmentRequests)
                        .ThenInclude(er => er.IdGroupNavigation)
                            .ThenInclude(g => g.IdSubjectNavigation)
                    .Include(s => s.IdGroups)
                        .ThenInclude(g => g.IdSubjectNavigation)
                    .Include(s => s.StudentsHasParents)
                        .ThenInclude(shp => shp.IdParentNavigation)
                            .ThenInclude(p => p.IdParentNavigation) // Загружаем пользователя для родителя
                    .Include(s => s.EnrollmentRequests)
                        .ThenInclude(er => er.IdParentNavigation)
                            .ThenInclude(p => p.IdParentNavigation) // Загружаем пользователя для родителя в заявках
                    .FirstOrDefaultAsync(s => s.IdStudent == id);

                if (student == null)
                {
                    return NotFound("Студент не найден");
                }

                var fullName = string.Join(" ",
                    new[] { student.IdStudentNavigation.Surname,
                           student.IdStudentNavigation.Name,
                           student.IdStudentNavigation.Patronymic }
                        .Where(p => !string.IsNullOrEmpty(p)));

                var result = new StudentDto
                {
                    IdStudent = student.IdStudent,
                    FullName = fullName,
                    Phone = student.IdStudentNavigation.Phone ?? "Не указан",
                    Login = student.IdStudentNavigation.Login,
                    EducationName = student.EducationName,
                    Parents = student.StudentsHasParents.Select(shp => new ParentDto
                    {
                        IdParent = shp.IdParentNavigation.IdParent,
                        FullName = string.Join(" ",
                            new[] { shp.IdParentNavigation.IdParentNavigation.Surname,
                                   shp.IdParentNavigation.IdParentNavigation.Name,
                                   shp.IdParentNavigation.IdParentNavigation.Patronymic }
                                .Where(n => !string.IsNullOrEmpty(n))),
                        Phone = shp.IdParentNavigation.IdParentNavigation.Phone ?? "Не указан",
                        BirthDate = shp.IdParentNavigation.IdParentNavigation.BirthDate,
                        ParentRole = shp.ParentRole,
                    }).ToList(),
                    EnrollmentRequests = student.EnrollmentRequests.Select(er => new EnrollmentRequestDto
                    {
                        IdRequest = er.IdRequests,
                        RequestDate = er.RequestDate,
                        Status = er.Status ?? "Нет статуса",
                        GroupName = er.IdGroupNavigation?.Name ?? "Неизвестная группа",
                        SubjectName = er.IdGroupNavigation?.IdSubjectNavigation?.Name ?? "Без предмета",
                        ParentFullName = string.Join(" ", new[]
                            {
                                er.IdParentNavigation.IdParentNavigation.Surname,
                                er.IdParentNavigation.IdParentNavigation.Name,
                                er.IdParentNavigation.IdParentNavigation.Patronymic
                            }),
                        IdParent = er.IdParentNavigation.IdParent,
                        Comment = er.Comment
                    }).ToList(),
                    Subjects = student.IdGroups
                        .GroupBy(g => g.IdSubjectNavigation)
                        .Select(g => new SubjectWithGroupsDto
                        {
                            IdSubject = g.Key.IdSubject,
                            Name = g.Key.Name,
                            Groups = g.Select(gr => new GroupDto
                            {
                                IdGroup = gr.IdGroup,
                                Name = gr.Name,
                                SubjectName = gr.IdSubjectNavigation.Name,
                            }).ToList()
                        }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении деталей студента с ID {Id}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // GET: api/Students/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Student>> GetStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.IdStudentNavigation) // Добавляем загрузку пользователя
                .FirstOrDefaultAsync(s => s.IdStudent == id);

            if (student == null)
            {
                return NotFound();
            }

            return student;
        }

        // PUT: api/Students/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudent(int id, Student student)
        {
            if (id != student.IdStudent)
            {
                return BadRequest();
            }

            _context.Entry(student).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Students
        [HttpPost]
        public async Task<ActionResult<Student>> PostStudent(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetStudent", new { id = student.IdStudent }, student);
        }

        // DELETE: api/Students/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.IdStudent == id);
        }
    }
}