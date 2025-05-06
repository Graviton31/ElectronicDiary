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
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using ElectronicDiaryApi.ModelsDto.UsersView;

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public EmployeesController(ElectronicDiaryContext context)
        {
            _context = context;
        }

        // GET: api/Employees
        [HttpGet]
        public async Task<ActionResult<PagedResponse<EmployeeDto>>> GetEmployees(int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.IdPostNavigation)
                    .AsQueryable();

                var totalCount = await query.CountAsync();
                var data = await query
                    .Select(e => new EmployeeDto
                    {
                        IdEmployee = e.IdEmployee,
                        FullName = e.Surname + " " + e.Name + " " + e.Patronymic,
                        Login = e.Login,
                        Phone = e.Phone,
                        Role = e.Role,
                        Post = e.IdPostNavigation.PostName
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new PagedResponse<EmployeeDto>
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
        public async Task<ActionResult<EmployeeDto>> GetEmployeeDetails(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.IdPostNavigation) // Должность
                .Include(e => e.IdSubjects)       // Предметы сотрудника
                .Include(e => e.IdGroups)         // Группы сотрудника (исправлено с Groups на IdGroups)
                .FirstOrDefaultAsync(e => e.IdEmployee == id);

            if (employee == null)
            {
                return NotFound();
            }

            // Формируем полное имя
            var fullName = string.Join(" ",
                new[] { employee.Surname, employee.Name, employee.Patronymic }
                    .Where(p => !string.IsNullOrEmpty(p)));

            // Создаем DTO
            var result = new EmployeeDto
            {
                IdEmployee = employee.IdEmployee,
                FullName = fullName,
                BirthDate = employee.BirthDate,
                Login = employee.Login,
                Role = employee.Role,
                Phone = employee.Phone,
                Post = employee.IdPostNavigation.PostName,
                Subjects = employee.IdSubjects
                    .Select(subject => new SubjectWithGroupsDto
                    {
                        IdSubject = subject.IdSubject,
                        Name = subject.Name,
                        Groups = employee.IdGroups 
                            .Where(g => g.IdSubject == subject.IdSubject) // Группы сотрудника по предмету
                            .Select(g => new GroupDto
                            {
                                IdGroup = g.IdGroup,
                                Name = g.Name,
                                Classroom = g.Classroom,
                            }).ToList()
                    }).ToList()
            };

            return Ok(result);
        }
         
        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            return employee;
        }

        // PUT: api/Employees/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmployee(int id, Employee employee)
        {
            if (id != employee.IdEmployee)
            {
                return BadRequest();
            }

            _context.Entry(employee).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
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

        // POST: api/Employees
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Employee>> PostEmployee(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEmployee", new { id = employee.IdEmployee }, employee);
        }

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.IdEmployee == id);
        }
    }
}
