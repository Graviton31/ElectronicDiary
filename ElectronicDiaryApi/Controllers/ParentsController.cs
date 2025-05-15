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

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParentsController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public ParentsController(ElectronicDiaryContext context)
        {
            _context = context;
        }

        // GET: api/Parents
        [HttpGet]
        public async Task<ActionResult<PagedResponse<ParentDto>>> GetParents(int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Parents
                    .AsQueryable();

                var totalCount = await query.CountAsync();
                var data = await query
                    .Select(p => new ParentDto
                    {
                        IdParent = p.IdParent,
                        FullName = p.Surname + " " + p.Name + " " + p.Patronymic,
                        Login = p.Login,
                        Phone = p.Phone,
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new PagedResponse<ParentDto>
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
        public async Task<ActionResult<ParentDto>> GetParentDetails(int id)
        {
            var parent = await _context.Parents
                .Include(p => p.EnrollmentRequests)
                .Include(p => p.IdStudents)
                .FirstOrDefaultAsync(p => p.IdParent == id);

            if (parent == null)
            {
                return NotFound();
            }

            var fullName = string.Join(" ",
                new[] { parent.Surname, parent.Name, parent.Patronymic }
                    .Where(p => !string.IsNullOrEmpty(p)));

            var result = new ParentDto
            {
                IdParent = parent.IdParent,
                FullName = fullName,
                BirthDate = parent.BirthDate,
                Phone = parent.Phone,
                Login = parent.Login,
                ParentRole = parent.ParentRole,
                Students = parent.IdStudents?.Select(s => new StudentDto
                {
                    IdStudent = s.IdStudent,
                    FullName = string.Join(" ",
                        new[] { s.Surname, s.Name, s.Patronymic }
                            .Where(p => !string.IsNullOrEmpty(p))),
                    Phone = s.Phone
                }).ToList() ?? new List<StudentDto>(), // Обработка null

                EnrollmentRequests = parent.EnrollmentRequests?.Select(er => new EnrollmentRequestDto
                {
                    IdRequest = er.IdRequests,
                    RequestDate = er.RequestDate,
                    Status = er.Status,
                    StudentFullName = string.Join(" ",
                        new[] { er.IdStudentNavigation?.Surname,
                                 er.IdStudentNavigation?.Name,
                                 er.IdStudentNavigation?.Patronymic }
                            .Where(p => !string.IsNullOrEmpty(p))),
                    IdStudent = er.IdStudent
                }).ToList() ?? new List<EnrollmentRequestDto>() // Обработка null
            };
            return Ok(result);
        }

        // GET: api/Parents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Parent>> GetParent(int id)
        {
            var parent = await _context.Parents.FindAsync(id);

            if (parent == null)
            {
                return NotFound();
            }

            return parent;
        }

        // PUT: api/Parents/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutParent(int id, Parent parent)
        {
            if (id != parent.IdParent)
            {
                return BadRequest();
            }

            _context.Entry(parent).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ParentExists(id))
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

        // POST: api/Parents
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Parent>> PostParent(Parent parent)
        {
            _context.Parents.Add(parent);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetParent", new { id = parent.IdParent }, parent);
        }

        // DELETE: api/Parents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteParent(int id)
        {
            var parent = await _context.Parents.FindAsync(id);
            if (parent == null)
            {
                return NotFound();
            }

            _context.Parents.Remove(parent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ParentExists(int id)
        {
            return _context.Parents.Any(e => e.IdParent == id);
        }
    }
}
