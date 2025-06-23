using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.ModelsDto.Group;
using ElectronicDiaryApi.ModelsDto.EnrollmentRequest;

namespace ElectronicDiaryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentRequestsController : ControllerBase
    {
        private readonly ElectronicDiaryContext _context;

        public EnrollmentRequestsController(ElectronicDiaryContext context)
        {
            _context = context;
        }

        // GET: Получение групп предмета с основной информацией
        [HttpGet("{subjectId}/groups")]
        public async Task<ActionResult<IEnumerable<GroupShortInfoDto>>> GetSubjectGroups(int subjectId)
        {
            var groups = await _context.Groups
                .Where(g => g.IdSubject == subjectId && g.IsDelete != true)
                .Select(g => new GroupShortInfoDto
                {
                    IdGroup = g.IdGroup,
                    Name = g.Name,
                    MaxStudentCount = g.MaxStudentCount,
                    CurrentStudents = g.IdStudents.Count,
                    RequestCount = _context.EnrollmentRequests
                            .Count(er => er.IdGroup == g.IdGroup)
                })
                .ToListAsync();

            return Ok(groups);
        }

        // GET: Получение заявок группы
        [HttpGet("groups/{groupId}/requests")]
        public async Task<ActionResult<IEnumerable<EnrollmentRequestDto>>> GetGroupRequests(int groupId)
        {
            var requests = await _context.EnrollmentRequests
                .Include(er => er.IdStudentNavigation)
                    .ThenInclude(s => s.IdStudentNavigation) // Загружаем данные пользователя студента
                .Include(er => er.IdParentNavigation)
                    .ThenInclude(p => p.IdParentNavigation) // Загружаем данные пользователя родителя
                .Include(er => er.IdGroupNavigation)
                    .ThenInclude(g => g.IdSubjectNavigation)
                .Where(er => er.IdGroup == groupId)
                .OrderByDescending(er => er.RequestDate)
                .Select(er => new EnrollmentRequestDto
                {
                    IdRequest = er.IdRequests,
                    RequestDate = er.RequestDate,
                    Status = er.Status,
                    Comment = er.Comment,
                    StudentFullName = $"{er.IdStudentNavigation.IdStudentNavigation.Surname} " +
                                    $"{er.IdStudentNavigation.IdStudentNavigation.Name} " +
                                    $"{er.IdStudentNavigation.IdStudentNavigation.Patronymic}".Trim(),
                    ParentFullName = $"{er.IdParentNavigation.IdParentNavigation.Surname} " +
                                   $"{er.IdParentNavigation.IdParentNavigation.Name} " +
                                   $"{er.IdParentNavigation.IdParentNavigation.Patronymic}".Trim(),
                    GroupName = er.IdGroupNavigation.Name,
                    SubjectName = er.IdGroupNavigation.IdSubjectNavigation.Name,
                    IdStudent = er.IdStudent,
                    IdParent = er.IdParent
                })
                .ToListAsync();

            return Ok(requests);
        }

        // PUT: Обновление статуса и комментария заявки
        [HttpPut("requests/{requestId}")]
        public async Task<IActionResult> UpdateRequest(int requestId, UpdateEnrollmentRequestDto updateDto)
        {
            var validStatuses = new[] { "ожидает", "одобрено", "отклонено" };
            if (!validStatuses.Contains(updateDto.Status))
                return BadRequest("Недопустимый статус заявки");

            var request = await _context.EnrollmentRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            request.Status = updateDto.Status;
            request.Comment = updateDto.Comment?.Trim(); // Разрешаем null

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RequestExists(requestId))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        private bool RequestExists(int id)
        {
            return _context.EnrollmentRequests.Any(e => e.IdRequests == id);
        }
    }
}