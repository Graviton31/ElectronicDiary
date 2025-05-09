using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto;
using ElectronicDiaryApi.ModelsDto.Responses;
using ElectronicDiaryApi.ModelsDto.UsersView;

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public StudentsController(ElectronicDiaryContext context)
        {
            _context = context;
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
                    .Include(s => s.IdParents)
                    .FirstOrDefaultAsync(s => s.IdStudent == id);

                if (student == null)
                {
                    return NotFound("Студент не найден");
                }

                var result = new StudentDto
                {
                    IdStudent = student.IdStudent,
                    FullName = string.Join(" ",
                        new[] { student.Surname, student.Name, student.Patronymic }
                            .Where(p => !string.IsNullOrEmpty(p))),
                    Phone = student.Phone ?? "Не указан",
                    Login = student.Login,
                    EducationName = student.EducationName,
                    Parents = student.IdParents.Select(p => new ParentDto
                    {
                        FullName = string.Join(" ",
                            new[] { p.Surname, p.Name, p.Patronymic }
                                .Where(n => !string.IsNullOrEmpty(n))),
                        Phone = p.Phone ?? "Не указан",
                        BirthDate = p.BirthDate,
                        ParentRole =p.ParentRole,
                    }).ToList(),
                    EnrollmentRequests = student.EnrollmentRequests.Select(er => new EnrollmentRequestDto
                    {
                        IdRequest = er.IdRequests,
                        RequestDate = er.RequestDate,
                        Status = er.Status ?? "Нет статуса",
                        GroupName = er.IdGroupNavigation?.Name ?? "Неизвестная группа",
                        SubjectName = er.IdGroupNavigation?.IdSubjectNavigation?.Name ?? "Без предмета",
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
                                Classroom = gr.Classroom,
                                SubjectName = gr.IdSubjectNavigation.Name,
                            }).ToList()
                        }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
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
