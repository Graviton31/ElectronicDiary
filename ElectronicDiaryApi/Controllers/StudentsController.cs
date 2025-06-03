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
        private readonly ILogger<GroupsController> _logger;

        public StudentsController(ElectronicDiaryContext context, ILogger<GroupsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Students
        [HttpGet]
        public async Task<ActionResult<PagedResponse<StudentDto>>> GetStudents(int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Students
                    .AsQueryable();

                var totalCount = await query.CountAsync();
                var data = await query
                    .Select(s => new StudentDto
                    {
                        IdStudent = s.IdStudent,
                        FullName = s.Surname + " " + s.Name + " " + s.Patronymic,
                        Login = s.Login,
                        Phone = s.Phone,
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

        // API Controller (StudentsController.cs)
        [HttpGet("Details/{id}")]
        public async Task<ActionResult<StudentDto>> GetStudentDetails(int id)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.EnrollmentRequests)
                        .ThenInclude(er => er.IdGroupNavigation)
                            .ThenInclude(g => g.IdSubjectNavigation)
                    .Include(s => s.IdGroups)
                        .ThenInclude(g => g.IdSubjectNavigation)
                    .Include(s => s.StudentsHasParents)  // Изменили с IdParents на StudentsHasParents
                        .ThenInclude(shp => shp.IdParentNavigation) // Добавили загрузку родителя
                    .Include(s => s.EnrollmentRequests)
                        .ThenInclude(er => er.IdParentNavigation)
                    .FirstOrDefaultAsync(s => s.IdStudent == id);

                if (student == null)
                {
                    return NotFound("Студент не найден");
                }

                var fullName = string.Join(" ",
                    new[] { student.Surname, student.Name, student.Patronymic }
                        .Where(p => !string.IsNullOrEmpty(p)));

                var result = new StudentDto
                {
                    IdStudent = student.IdStudent,
                    FullName = fullName,
                    Phone = student.Phone ?? "Не указан",
                    Login = student.Login,
                    EducationName = student.EducationName,
                    Parents = student.StudentsHasParents.Select(shp => new ParentDto
                    {
                        IdParent = shp.IdParentNavigation.IdParent,
                        FullName = string.Join(" ",
                            new[] { shp.IdParentNavigation.Surname,
                           shp.IdParentNavigation.Name,
                           shp.IdParentNavigation.Patronymic }
                                .Where(n => !string.IsNullOrEmpty(n))),
                        Phone = shp.IdParentNavigation.Phone ?? "Не указан",
                        BirthDate = shp.IdParentNavigation.BirthDate,
                        ParentRole = shp.ParentRole, // Теперь роль берется из связи, а не из родителя
                    }).ToList(),
                    EnrollmentRequests = student.EnrollmentRequests.Select(er => new EnrollmentRequestDto
                    {
                        IdRequest = er.IdRequests,
                        RequestDate = er.RequestDate,
                        Status = er.Status ?? "Нет статуса",
                        GroupName = er.IdGroupNavigation?.Name ?? "Неизвестная группа",
                        SubjectName = er.IdGroupNavigation?.IdSubjectNavigation?.Name ?? "Без предмета",
                        ParentFullName = string.Join(" ", new[]
                            { er.IdParentNavigation.Surname, er.IdParentNavigation.Name, er.IdParentNavigation.Patronymic }),
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
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            return student;
        }

        // PUT: api/Students/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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
