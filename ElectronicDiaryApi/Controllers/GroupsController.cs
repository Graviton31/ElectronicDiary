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
    public class GroupsController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public GroupsController(ElectronicDiaryContext context)
        {
            _context = context;
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
                StudentCount = createDto.StudentCount,
                Classroom = createDto.Classroom,
                IdSubject = createDto.IdSubject,
                IdLocation = createDto.IdLocation
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
                .FirstOrDefaultAsync(g => g.IdGroup == id);

            if (group == null) return NotFound();

            return new GroupDto
            {
                IdGroup = group.IdGroup,
                Name = group.Name,
                Classroom = group.Classroom,
                IdSubject = group.IdSubject,
                Location = new LocationDto
                {
                    IdLocation = group.IdLocationNavigation.IdLocation,
                    Name = group.IdLocationNavigation.Name,
                    Address = group.IdLocationNavigation.Addres
                }
            };
        }
    }
}