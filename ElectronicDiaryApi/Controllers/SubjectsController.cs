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
using ElectronicDiaryApi.ModelsDto.UsersView;

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectsController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public SubjectsController(ElectronicDiaryContext context)
        {
            _context = context;
        }

        // GET: api/Subjects
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubjectListItemDto>>> GetSubjects()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Groups)
                .Include(s => s.IdEmployees)
                .Where(s => !s.IsDelete)
                .Select(s => new SubjectListItemDto
                {
                    IdSubject = s.IdSubject,
                    Name = s.Name,
                    FullName = s.FullName,
                    GroupsCount = s.Groups.Count,
                    TeachersCount = s.IdEmployees.Count
                })
                .ToListAsync();

            return Ok(subjects);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SubjectDto>> GetSubject(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.IdEmployees)
                .FirstOrDefaultAsync(s => s.IdSubject == id);

            if (subject == null) return NotFound();

            return new SubjectDto
            {
                IdSubject = subject.IdSubject,
                Name = subject.Name,
                FullName = subject.FullName,
                Description = subject.Description,
                Duration = subject.Duration,
                LessonLength = subject.LessonLength,
                Teachers = subject.IdEmployees.Select(t => new EmployeeDto
                {
                    IdEmployee = t.IdEmployee,
                    FullName = $"{t.Surname} {t.Name} {t.Patronymic}".Trim(),
                    Phone = t.Phone, 
                    Login = t.Login
                }).ToList()
            };
        }

        [HttpGet("{id}/groups")]
        public async Task<ActionResult<List<GroupDto>>> GetSubjectGroups(int id)
        {
            return await _context.Groups
                .Where(g => g.IdSubject == id)
                .Include(g => g.IdLocationNavigation)
                .Include(g => g.IdEmployees)
                .Include(g => g.IdStudents)
                .Select(g => new GroupDto
                {
                    IdGroup = g.IdGroup,
                    Name = g.Name,
                    Classroom = g.Classroom,
                    Location = new LocationDto
                    {
                        IdLocation = g.IdLocationNavigation.IdLocation,
                        Name = g.IdLocationNavigation.Name,
                        Address = g.IdLocationNavigation.Addres
                    },
                    Teachers = g.IdEmployees.Select(t => new EmployeeDto
                    {
                        IdEmployee = t.IdEmployee,
                        FullName = $"{t.Surname} {t.Name} {t.Patronymic}".Trim(),
                    }).ToList(),
                    MaxStudentCount = g.MaxStudentCount ?? 0,
                    CurrentStudents = g.IdStudents.Count,
                    IdSubject = g.IdSubject,
                    SubjectName = g.IdSubjectNavigation.Name
                })
                .ToListAsync();
        }

        [HttpPost("create")]
        public async Task<ActionResult<Subject>> CreateSubject(CreateSubjectDto createDto)
        {
            // Проверка существования предмета
            var existingSubject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Name == createDto.Name);

            if (existingSubject != null)
            {
                return Conflict(new { message = "Предмет с таким названием уже существует" });
            }

            var subject = new Subject
            {
                Name = createDto.Name,
                FullName = createDto.FullName,
                Description = createDto.Description,
                Duration = createDto.Duration,
                LessonLength = createDto.LessonLength,
                IsDelete = false // По умолчанию не удален
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubject), new { id = subject.IdSubject }, subject);
        }

        [HttpPost("{subjectId}/teachers/{teacherId}")]
        public async Task<IActionResult> AddTeacherToSubject(int subjectId, int teacherId)
        {
            var subject = await _context.Subjects.FindAsync(subjectId);
            var teacher = await _context.Employees.FindAsync(teacherId);

            if (subject == null || teacher == null) return NotFound();

            subject.IdEmployees.Add(teacher);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // PUT: api/Subjects/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSubject(int id, Subject subject)
        {
            if (id != subject.IdSubject)
            {
                return BadRequest();
            }

            _context.Entry(subject).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubjectExists(id))
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

        // DELETE: api/Subjects/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                return NotFound();
            }

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.IdSubject == id);
        }
    }
}
