using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto;
using ElectronicDiaryApi.ModelsDto.UsersView;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicDiaryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(ElectronicDiaryContext context, ILogger<GroupsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<GroupDto>> CreateGroup(CreateGroupDto createDto)
        {
            var subject = await _context.Subjects.FindAsync(createDto.IdSubject);
            if (subject == null) return BadRequest("Invalid Subject ID");

            var location = await _context.Locations.FindAsync(createDto.IdLocation);
            if (location == null) return BadRequest("Invalid Location ID");

            var group = new Group
            {
                Name = createDto.Name,
                MaxStudentCount = createDto.MaxStudentCount,
                Classroom = createDto.Classroom,
                IdSubject = createDto.IdSubject,
                IdLocation = createDto.IdLocation,
                MinAge = createDto.MinAge,
                MaxAge = createDto.MaxAge,
            };

            // Добавляем учителей к группе
            foreach (var teacherId in createDto.TeacherIds)
            {
                var teacher = await _context.Employees.FindAsync(teacherId);
                if (teacher != null) group.IdEmployees.Add(teacher);
            }

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // Явная загрузка связанных данных
            await _context.Entry(group)
                .Reference(g => g.IdLocationNavigation)
                .LoadAsync();

            return CreatedAtAction(nameof(GetGroup), new { id = group.IdGroup },
                new GroupDto
                {
                    IdGroup = group.IdGroup,
                    Name = group.Name,
                    Classroom = group.Classroom,
                    IdSubject = group.IdSubject,
                    MinAge = createDto.MinAge,
                    MaxAge = createDto.MaxAge,
                    Location = new LocationDto
                    {
                        IdLocation = group.IdLocationNavigation.IdLocation,
                        Name = group.IdLocationNavigation.Name,
                        Address = group.IdLocationNavigation.Addres
                    }
                });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GroupDto>> GetGroup(int id)
        {
            var group = await _context.Groups
                .Include(g => g.IdLocationNavigation)
                .Where(g => g.IsDelete != true)
                .FirstOrDefaultAsync(g => g.IdGroup == id);

            if (group == null) return NotFound();

            return new GroupDto
            {
                IdGroup = group.IdGroup,
                Name = group.Name,
                Classroom = group.Classroom,
                IdSubject = group.IdSubject,
                MinAge = group.MinAge,
                MaxAge = group.MaxAge,
                Location = new LocationDto
                {
                    IdLocation = group.IdLocationNavigation.IdLocation,
                    Name = group.IdLocationNavigation.Name,
                    Address = group.IdLocationNavigation.Addres
                }
            };
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutGroup(int id, UpdateGroupDto updateDto)
        {
            var location = await _context.Locations.FindAsync(updateDto.IdLocation);
            if (location == null) return BadRequest("Invalid Location ID");

            var group = await _context.Groups
                .Include(g => g.IdEmployees)
                .FirstOrDefaultAsync(g => g.IdGroup == id);

            if (group == null) return NotFound();

            // Обновление основных полей
            group.Name = updateDto.Name;
            group.Classroom = updateDto.Classroom;
            group.MaxStudentCount = updateDto.MaxStudentCount;
            group.MinAge = updateDto.MinAge;
            group.MaxAge = updateDto.MaxAge;
            group.IdLocation = updateDto.IdLocation;

            // Обновление преподавателей
            var currentTeachers = group.IdEmployees.ToList();
            var selectedIds = updateDto.TeacherIds ?? new List<int>();

            // Удаляем неактуальных преподавателей
            foreach (var teacher in currentTeachers)
            {
                if (!selectedIds.Contains(teacher.IdEmployee))
                {
                    group.IdEmployees.Remove(teacher);
                }
            }

            // Добавляем новых преподавателей
            foreach (var teacherId in selectedIds)
            {
                if (!currentTeachers.Any(t => t.IdEmployee == teacherId))
                {
                    var teacher = await _context.Employees.FindAsync(teacherId);
                    if (teacher != null) group.IdEmployees.Add(teacher);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GroupExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            try
            {
                var group = await _context.Groups.FindAsync(id);
                if (group == null)
                {
                    _logger.LogWarning("Group not found: {GroupId}", id);
                    return NotFound();
                }

                group.IsDelete = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Group {GroupId} marked as deleted", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group {GroupId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        private bool GroupExists(int id)
        {
            return _context.Groups.Any(e => e.IdGroup == id);
        }
    }
}