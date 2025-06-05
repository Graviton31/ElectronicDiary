using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto.EnrollmentRequest;
using ElectronicDiaryApi.ModelsDto.Group;
using ElectronicDiaryApi.ModelsDto.Responses;
using ElectronicDiaryApi.ModelsDto.Subject;
using ElectronicDiaryApi.ModelsDto.UsersView;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            ElectronicDiaryContext context,
            ILogger<EmployeesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Employees
        [HttpGet]
        public async Task<ActionResult<PagedResponse<EmployeeDto>>> GetEmployees(int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.IdEmployeeNavigation) // Добавлено для загрузки пользователя
                    .Include(e => e.IdPostNavigation)
                    .AsQueryable();

                var totalCount = await query.CountAsync();
                var data = await query
                    .Select(e => new EmployeeDto
                    {
                        IdEmployee = e.IdEmployee,
                        FullName = e.IdEmployeeNavigation.Surname + " " +
                                 e.IdEmployeeNavigation.Name + " " +
                                 e.IdEmployeeNavigation.Patronymic,
                        Login = e.IdEmployeeNavigation.Login,
                        Phone = e.IdEmployeeNavigation.Phone,
                        Role = e.IdEmployeeNavigation.Role,
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

        [HttpGet("all-short")]
        public async Task<ActionResult<IEnumerable<EnrollmentRequestShortInfoDto>>> GetAllEmployeesShortInfo()
        {
            try
            {
                var employees = await _context.Employees
                    .Include(e => e.IdEmployeeNavigation) // Добавлено для загрузки пользователя
                    .Select(e => new EnrollmentRequestShortInfoDto
                    {
                        IdEmployee = e.IdEmployee,
                        FullName = $"{e.IdEmployeeNavigation.Surname} {e.IdEmployeeNavigation.Name} {e.IdEmployeeNavigation.Patronymic}".Trim()
                    })
                    .ToListAsync();

                return Ok(employees);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<EnrollmentRequestShortInfoDto>>> SearchEmployees(string term)
        {
            try
            {
                var searchTerm = term?.Trim();
                if (string.IsNullOrEmpty(searchTerm))
                    return Ok(new List<EnrollmentRequestShortInfoDto>());

                var query = _context.Employees
                    .Include(e => e.IdEmployeeNavigation) // Добавлено для загрузки пользователя
                    .Where(e => e.IdEmployeeNavigation.IsDelete != true)
                    .AsQueryable();

                var employees = await query
                    .Where(e =>
                        EF.Functions.Like(
                            (e.IdEmployeeNavigation.Surname ?? "") + " " +
                            (e.IdEmployeeNavigation.Name ?? "") + " " +
                            (e.IdEmployeeNavigation.Patronymic ?? ""),
                            $"%{searchTerm}%") ||
                        EF.Functions.Like(
                            (e.IdEmployeeNavigation.Name ?? "") + " " +
                            (e.IdEmployeeNavigation.Surname ?? "") + " " +
                            (e.IdEmployeeNavigation.Patronymic ?? ""),
                            $"%{searchTerm}%")
                    )
                    .OrderBy(e => e.IdEmployeeNavigation.Surname)
                    .Take(10)
                    .Select(e => new EnrollmentRequestShortInfoDto
                    {
                        IdEmployee = e.IdEmployee,
                        FullName = $"{e.IdEmployeeNavigation.Surname} {e.IdEmployeeNavigation.Name} {e.IdEmployeeNavigation.Patronymic}".Trim()
                    })
                    .AsSplitQuery()
                    .ToListAsync();

                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска преподавателей. Term: {Term}", term);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("Details/{id}")]
        public async Task<ActionResult<EmployeeDto>> GetEmployeeDetails(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .AsSplitQuery()
                    .Include(e => e.IdEmployeeNavigation) // Добавлено для загрузки пользователя
                    .Include(e => e.IdPostNavigation)
                    .Include(e => e.IdSubjects)
                    .Include(e => e.IdGroups)
                        .ThenInclude(g => g.IdSubjectNavigation)
                    .FirstOrDefaultAsync(e => e.IdEmployee == id);

                if (employee == null) return NotFound();

                var fullName = string.Join(" ", new[]
                {
                    employee.IdEmployeeNavigation.Surname,
                    employee.IdEmployeeNavigation.Name,
                    employee.IdEmployeeNavigation.Patronymic
                }.Where(s => !string.IsNullOrEmpty(s)));

                return new EmployeeDto
                {
                    IdEmployee = employee.IdEmployee,
                    FullName = fullName,
                    BirthDate = employee.IdEmployeeNavigation.BirthDate,
                    Login = employee.IdEmployeeNavigation.Login,
                    Role = employee.IdEmployeeNavigation.Role,
                    Phone = employee.IdEmployeeNavigation.Phone,
                    Post = employee.IdPostNavigation?.PostName ?? "Не указано",
                    Subjects = employee.IdSubjects.Select(subject => new SubjectWithGroupsDto
                    {
                        IdSubject = subject.IdSubject,
                        Name = subject.Name,
                        Groups = employee.IdGroups
                            .Where(g => g.IdSubject == subject.IdSubject)
                            .Select(g => new GroupDto
                            {
                                IdGroup = g.IdGroup,
                                Name = g.Name,
                                SubjectName = g.IdSubjectNavigation?.Name
                            }).ToList()
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee details for ID: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.IdEmployeeNavigation) // Добавлено для загрузки пользователя
                    .FirstOrDefaultAsync(e => e.IdEmployee == id);
                return employee != null ? employee : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee with ID: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

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

        [HttpPost]
        public async Task<ActionResult<Employee>> PostEmployee(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEmployee", new { id = employee.IdEmployee }, employee);
        }

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