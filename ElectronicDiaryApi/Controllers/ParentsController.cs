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
        public async Task<ActionResult<PagedResponse<ParentDto>>> GetParents(
            int page = 1,
            int pageSize = 10,
            string search = "")
        {
            try
            {
                var query = _context.Parents
                    .Include(p => p.IdParentNavigation)
                    .Where(p => p.IdParentNavigation.IsDelete != true)
                    .AsQueryable();

                // Добавляем поиск
                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.ToLower();
                    query = query.Where(p =>
                        (p.IdParentNavigation.Surname + " " +
                         p.IdParentNavigation.Name + " " +
                         p.IdParentNavigation.Patronymic).ToLower().Contains(searchTerm) ||
                        p.IdParentNavigation.Login.ToLower().Contains(searchTerm) ||
                        (p.Workplace != null && p.Workplace.ToLower().Contains(searchTerm))
                    );
                }

                var totalCount = await query.CountAsync();
                var data = await query
                    .Select(p => new ParentDto
                    {
                        IdParent = p.IdParent,
                        FullName = p.IdParentNavigation.Surname + " " +
                                 p.IdParentNavigation.Name + " " +
                                 p.IdParentNavigation.Patronymic,
                        Login = p.IdParentNavigation.Login,
                        Phone = p.IdParentNavigation.Phone,
                        Workplace = p.Workplace
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
                .Include(p => p.IdParentNavigation) // Загружаем данные пользователя
                .Include(p => p.EnrollmentRequests)
                    .ThenInclude(er => er.IdStudentNavigation)
                        .ThenInclude(s => s.IdStudentNavigation) // Загружаем пользователя студента
                .Include(p => p.StudentsHasParents)
                    .ThenInclude(shp => shp.IdStudentNavigation)
                        .ThenInclude(s => s.IdStudentNavigation) // Загружаем пользователя студента
                .FirstOrDefaultAsync(p => p.IdParent == id);

            if (parent == null)
            {
                return NotFound();
            }

            var fullName = string.Join(" ",
                new[] { parent.IdParentNavigation.Surname,
                       parent.IdParentNavigation.Name,
                       parent.IdParentNavigation.Patronymic }
                    .Where(p => !string.IsNullOrEmpty(p)));

            var result = new ParentDto
            {
                IdParent = parent.IdParent,
                FullName = fullName,
                BirthDate = parent.IdParentNavigation.BirthDate,
                Phone = parent.IdParentNavigation.Phone,
                Login = parent.IdParentNavigation.Login,
                Workplace = parent.Workplace,
                Students = parent.StudentsHasParents.Select(shp => new StudentDto
                {
                    IdStudent = shp.IdStudentNavigation.IdStudent,
                    FullName = string.Join(" ",
                        new[] { shp.IdStudentNavigation.IdStudentNavigation.Surname,
                               shp.IdStudentNavigation.IdStudentNavigation.Name,
                               shp.IdStudentNavigation.IdStudentNavigation.Patronymic }
                            .Where(p => !string.IsNullOrEmpty(p))),
                    Phone = shp.IdStudentNavigation.IdStudentNavigation.Phone,
                    ParentRole = shp.ParentRole,
                    EducationName = shp.IdStudentNavigation.EducationName
                }).ToList(),

                EnrollmentRequests = parent.EnrollmentRequests.Select(er => new EnrollmentRequestDto
                {
                    IdRequest = er.IdRequests,
                    RequestDate = er.RequestDate,
                    Status = er.Status,
                    StudentFullName = string.Join(" ",
                        new[] { er.IdStudentNavigation?.IdStudentNavigation?.Surname,
                               er.IdStudentNavigation?.IdStudentNavigation?.Name,
                               er.IdStudentNavigation?.IdStudentNavigation?.Patronymic }
                            .Where(p => !string.IsNullOrEmpty(p))),
                    IdStudent = er.IdStudent
                }).ToList()
            };

            return Ok(result);
        }

        // GET: api/Parents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Parent>> GetParent(int id)
        {
            var parent = await _context.Parents
                .Include(p => p.IdParentNavigation) // Добавляем загрузку пользователя
                .FirstOrDefaultAsync(p => p.IdParent == id);

            if (parent == null)
            {
                return NotFound();
            }

            return parent;
        }

        // PUT: api/Parents/5
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